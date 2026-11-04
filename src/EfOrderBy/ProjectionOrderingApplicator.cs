/// <summary>
/// Visitor that applies default ordering to the collection navigations a Select projection loads.
/// </summary>
/// <remarks>
/// A collection navigation read inside a projection, for example
/// Select(_ => new { Employees = _.Employees.Where(...).Select(...).ToList() }), is loaded by a
/// subquery of its own. Without ordering, EF Core returns that subquery's rows in key order, so
/// the default ordering configured for the collection's entity was ignored, the same as it was for
/// an Include before IncludeOrderingApplicator.
/// The ordering is placed after any Where filters and before the operators that project the entity
/// away or choose which rows come back, mirroring how the outer query is ordered. A collection
/// whose order cannot affect the result, such as one only counted or tested with Any, is left as
/// it is, as is one composed with an operator the ordering could not be placed beneath.
/// </remarks>
sealed class ProjectionOrderingApplicator(IModel model, bool detectRedundantOrdering) :
    ExpressionVisitor
{
    static readonly ConcurrentDictionary<Type, MethodInfo> toListMethodCache = new();

    static readonly MethodInfo toListMethod = typeof(Enumerable)
        .GetMethod(nameof(Enumerable.ToList))!;

    // Lambdas of the Select projections currently being visited. Ordering is only applied inside one,
    // since that is where a collection is loaded into the result. A collection read in a Where or
    // OrderBy lambda of the outer query only filters or sorts it.
    int projectionDepth;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsQueryableSelect(node))
        {
            return VisitSelect(node);
        }

        if (projectionDepth > 0 &&
            TryFindNavigationChain(node, out var navigation))
        {
            return ProcessChain(node, navigation);
        }

        return base.VisitMethodCall(node);
    }

    static bool IsQueryableSelect(MethodCallExpression node) =>
        node.Method.DeclaringType == typeof(Queryable) &&
        node.Method.Name == nameof(Queryable.Select) &&
        node.Arguments is [_, UnaryExpression { Operand: LambdaExpression }];

    Expression VisitSelect(MethodCallExpression node)
    {
        var source = Visit(node.Arguments[0]);

        var quote = (UnaryExpression) node.Arguments[1];
        projectionDepth++;
        Expression selector;
        try
        {
            selector = Visit(quote);
        }
        finally
        {
            projectionDepth--;
        }

        return node.Update(node.Object, [source, selector]);
    }

    // A navigation bound into the result as it is, for example new { _.Employees }, is loaded the
    // same way as one read through a chain of operators, so it is ordered as well. It is replaced
    // by the ordered navigation copied into a list, which only fits where a list can stand in for
    // the navigation's declared type.
    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        var visited = base.VisitMemberAssignment(node);
        if (projectionDepth == 0)
        {
            return visited;
        }

        return visited.Update(OrderBoundNavigation(visited.Expression));
    }

    protected override Expression VisitNew(NewExpression node)
    {
        var visited = (NewExpression) base.VisitNew(node);
        if (projectionDepth == 0)
        {
            return visited;
        }

        Expression[]? arguments = null;
        for (var index = 0; index < visited.Arguments.Count; index++)
        {
            var argument = visited.Arguments[index];
            var ordered = OrderBoundNavigation(argument);
            if (ReferenceEquals(ordered, argument))
            {
                continue;
            }

            arguments ??= visited.Arguments.ToArray();
            arguments[index] = ordered;
        }

        if (arguments == null)
        {
            return visited;
        }

        return visited.Update(arguments);
    }

    Expression OrderBoundNavigation(Expression expression)
    {
        if (expression is not MemberExpression member ||
            FindCollectionNavigation(member) is not { } navigation ||
            GetConfiguration(navigation) is not { } configuration)
        {
            return expression;
        }

        var elementType = navigation.TargetEntityType.ClrType;
        var listType = typeof(List<>).MakeGenericType(elementType);
        if (!expression.Type.IsAssignableFrom(listType))
        {
            return expression;
        }

        var toList = toListMethodCache.GetOrAdd(
            elementType,
            static _ => toListMethod.MakeGenericMethod(_));
        return Expression.Call(toList, AppendOrdering(expression, configuration));
    }

    /// <summary>
    /// Whether <paramref name="node"/> is the outermost operator of a chain over a collection
    /// navigation, such as _.Employees.Where(...).Select(...).ToList().
    /// </summary>
    bool TryFindNavigationChain(MethodCallExpression node, [NotNullWhen(true)] out INavigationBase? navigation)
    {
        navigation = null;
        Expression current = node;
        while (current is MethodCallExpression call)
        {
            if (!IsChainOperator(call))
            {
                return false;
            }

            current = call.Arguments[0];
        }

        if (current is not MemberExpression member)
        {
            return false;
        }

        navigation = FindCollectionNavigation(member);
        return navigation != null;
    }

    static bool IsChainOperator(MethodCallExpression call) =>
        call.Method.IsStatic &&
        call.Method.DeclaringType == typeof(Enumerable) &&
        call.Arguments.Count > 0;

    INavigationBase? FindCollectionNavigation(MemberExpression member)
    {
        if (member.Expression == null)
        {
            return null;
        }

        var entity = model.FindEntityType(member.Expression.Type);
        if (entity == null)
        {
            return null;
        }

        var name = member.Member.Name;
        INavigationBase? navigation = entity.FindNavigation(name);
        navigation ??= entity.FindSkipNavigation(name);
        if (navigation is not { IsCollection: true })
        {
            return null;
        }

        return navigation;
    }

    Expression ProcessChain(MethodCallExpression node, INavigationBase navigation)
    {
        // Lambdas passed along the chain can load collections of their own, for example
        // _.Employees.Select(employee => new { Tasks = employee.Tasks.ToList() })
        var chain = VisitChainArguments(node);

        if (QueryAnalyzer.HasOrdering(chain))
        {
            if (detectRedundantOrdering)
            {
                RedundantOrder.Validate(chain);
            }

            return chain;
        }

        if (GetConfiguration(navigation) is not { } configuration ||
            !CanOrder(chain))
        {
            return chain;
        }

        return ApplyOrdering(chain, configuration);
    }

    // Visits every argument of each operator in the chain except the source it composes onto, which
    // is the next operator down or the navigation itself. Visiting the navigation as a member would
    // order it a second time as if it were bound into the result.
    Expression VisitChainArguments(Expression expression)
    {
        if (expression is not MethodCallExpression call)
        {
            return expression;
        }

        var arguments = new Expression[call.Arguments.Count];
        arguments[0] = VisitChainArguments(call.Arguments[0]);
        for (var index = 1; index < arguments.Length; index++)
        {
            arguments[index] = VisitChainArgument(call.Arguments[index]);
        }

        return call.Update(call.Object, arguments);
    }

    // A lambda passed to the chain, such as a Select selector, can load collections into the result.
    // Any other argument is not part of the result as it is: the second sequence of a Concat or a
    // Union is one operand of a set operation, and ordering an operand orders nothing.
    Expression VisitChainArgument(Expression argument)
    {
        if (argument is LambdaExpression)
        {
            return Visit(argument);
        }

        var depth = projectionDepth;
        projectionDepth = 0;
        try
        {
            return Visit(argument);
        }
        finally
        {
            projectionDepth = depth;
        }
    }

    /// <summary>
    /// Operators the default ordering has to be applied beneath rather than after.
    /// </summary>
    /// <remarks>
    /// Select projects the entity away, Skip, Take and the single result operators choose which
    /// rows come back, and the rest copy the sequence as it is.
    /// </remarks>
    static bool ShouldOrderBefore(MethodCallExpression call) =>
        call.Method.Name is
            nameof(Enumerable.Select) or
            nameof(Enumerable.Skip) or
            nameof(Enumerable.Take) or
            nameof(Enumerable.First) or
            nameof(Enumerable.FirstOrDefault) or
            nameof(Enumerable.Last) or
            nameof(Enumerable.LastOrDefault) or
            nameof(Enumerable.ElementAt) or
            nameof(Enumerable.ElementAtOrDefault) or
            nameof(Enumerable.ToList) or
            nameof(Enumerable.ToArray) or
            nameof(Enumerable.AsEnumerable);

    /// <summary>
    /// Whether the ordering can go in beneath the operators that come after it, with only Where
    /// filters between it and the navigation. Anything else, an aggregate such as Count or Any, or
    /// an operator such as Distinct that EF Core drops ordering beneath, means the order of the
    /// collection does not reach the result.
    /// </summary>
    static bool CanOrder(Expression chain)
    {
        var current = chain;
        while (current is MethodCallExpression call &&
               ShouldOrderBefore(call))
        {
            current = call.Arguments[0];
        }

        while (current is MethodCallExpression call)
        {
            if (call.Method.Name != nameof(Enumerable.Where))
            {
                return false;
            }

            current = call.Arguments[0];
        }

        return true;
    }

    static Expression ApplyOrdering(Expression chain, Configuration configuration)
    {
        // Push past everything the ordering has to precede, then rebuild the chain around it
        if (chain is MethodCallExpression call &&
            ShouldOrderBefore(call))
        {
            var arguments = call.Arguments.ToArray();
            arguments[0] = ApplyOrdering(arguments[0], configuration);
            return call.Update(call.Object, arguments);
        }

        return AppendOrdering(chain, configuration);
    }

    static Expression AppendOrdering(Expression source, Configuration configuration)
    {
        var result = source;

        foreach (var clause in configuration.Clauses)
        {
            result = clause.AppendEnumerableOrder(result);
        }

        return result;
    }

    static Configuration? GetConfiguration(INavigationBase navigation)
    {
        var target = navigation.TargetEntityType;

        // A JSON mapped collection is read out of its parent's JSON document rather than joined,
        // so its document order is left as it is, the same as for an Include
        if (target.IsMappedToJson())
        {
            return null;
        }

        if (Configuration.TryGet(target.ClrType) is not { Clauses.Count: > 0 } configuration)
        {
            return null;
        }

        return configuration;
    }
}
