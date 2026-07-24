/// <summary>
/// Detects explicit ordering in a query that exactly duplicates the configured default ordering.
/// </summary>
static class RedundantOrder
{
    public static void Validate(Expression expression)
    {
        if (FindOrdering(expression) is not { } ordering)
        {
            return;
        }

        var (elementType, clauses) = ordering;

        if (Configuration.TryGet(elementType) is not { Clauses.Count: > 0 } configuration)
        {
            return;
        }

        if (!configuration.ClauseMetadataList.SequenceEqual(clauses))
        {
            return;
        }

        throw new(
            $"""
             The query explicitly orders '{elementType.Name}' by {Describe(clauses)}, which exactly matches the default ordering configured for that entity.
             Remove the ordering from the query since it is applied automatically, or change it to a different ordering.
             """);
    }

    /// <summary>
    /// Walks the outermost ordering chain of a query and returns the ordered element type
    /// along with the clauses in the order they are applied. Returns null when the query has
    /// no ordering, or when a clause cannot be expressed as default ordering configuration.
    /// </summary>
    static (Type ElementType, List<Configuration.ClauseMetadata> Clauses)? FindOrdering(Expression expression)
    {
        var clauses = new List<Configuration.ClauseMetadata>();
        Type? elementType = null;

        while (expression is MethodCallExpression call)
        {
            var method = call.Method;
            if (IsOrderingMethod(method, out var descending, out var isThenBy))
            {
                // The overloads taking an IComparer cannot be expressed as configuration
                if (call.Arguments.Count != 2)
                {
                    return null;
                }

                if (FindPropertyName(call.Arguments[1]) is not { } propertyName)
                {
                    return null;
                }

                clauses.Add(new(propertyName, descending, isThenBy));
                elementType = method.GetGenericArguments()[0];
                expression = call.Arguments[0];
                continue;
            }

            // The chain ends at the first non ordering call
            if (clauses.Count > 0)
            {
                break;
            }

            if (FindSource(call) is not { } source)
            {
                return null;
            }

            expression = source;
        }

        if (elementType == null)
        {
            return null;
        }

        // The chain is walked outermost first, so the clauses come out in reverse of how they apply
        clauses.Reverse();
        return (elementType, clauses);
    }

    // Operators that combine two sequences. Their first argument is only one side of the
    // query, and the default ordering is never applied to the combined result, so ordering
    // found down that side is not made redundant by it.
    static readonly HashSet<string> combining =
    [
        "Concat",
        "Except",
        "ExceptBy",
        "GroupJoin",
        "Intersect",
        "IntersectBy",
        "Join",
        "LeftJoin",
        "RightJoin",
        "SequenceEqual",
        "Union",
        "UnionBy",
        "Zip"
    ];

    // Ordering can sit behind calls like Where, AsNoTracking, or TagWith,
    // all of which take the query being composed as their first argument.
    static Expression? FindSource(MethodCallExpression call)
    {
        var method = call.Method;
        if (method.IsStatic &&
            call.Arguments.Count > 0 &&
            !combining.Contains(method.Name) &&
            typeof(IEnumerable).IsAssignableFrom(call.Arguments[0].Type))
        {
            return call.Arguments[0];
        }

        return null;
    }

    static bool IsOrderingMethod(MethodInfo method, out bool descending, out bool isThenBy)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == typeof(Queryable) ||
            declaringType == typeof(Enumerable))
        {
            switch (method.Name)
            {
                case "OrderBy":
                    descending = false;
                    isThenBy = false;
                    return true;
                case "OrderByDescending":
                    descending = true;
                    isThenBy = false;
                    return true;
                case "ThenBy":
                    descending = false;
                    isThenBy = true;
                    return true;
                case "ThenByDescending":
                    descending = true;
                    isThenBy = true;
                    return true;
            }
        }

        descending = false;
        isThenBy = false;
        return false;
    }

    // Queryable methods take a quoted lambda, Enumerable methods take the lambda directly
    static string? FindPropertyName(Expression keySelector)
    {
        if (keySelector is UnaryExpression
            {
                NodeType: ExpressionType.Quote,
                Operand: LambdaExpression quoted
            })
        {
            keySelector = quoted;
        }

        if (keySelector is LambdaExpression
            {
                Body: MemberExpression
                {
                    Expression: ParameterExpression,
                    Member: PropertyInfo property
                }
            })
        {
            return property.Name;
        }

        return null;
    }

    static string Describe(List<Configuration.ClauseMetadata> clauses) =>
        string.Join('.', clauses.Select(_ => $"{MethodName(_)}({_.PropertyName})"));

    static string MethodName(Configuration.ClauseMetadata clause) =>
        (clause.IsThenBy, clause.Descending) switch
        {
            (false, false) => "OrderBy",
            (false, true) => "OrderByDescending",
            (true, false) => "ThenBy",
            _ => "ThenByDescending"
        };
}
