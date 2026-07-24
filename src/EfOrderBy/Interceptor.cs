/// <summary>
/// Interceptor that applies default ordering to queries that don't have explicit OrderBy.
/// </summary>
sealed class Interceptor : IQueryExpressionInterceptor
{
    static readonly ConcurrentDictionary<Type, Type?> queryElementTypeCache = new();

    public Expression QueryCompilationStarting(Expression query, QueryExpressionEventData eventData)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return query;
        }

        var model = context.Model;
        RequiredOrder.Validate(context);

        var detectRedundantOrdering = RedundantOrder.IsEnabled(context);

        // First, process Include nodes to add ordering to nested collections
        var visitor = new IncludeOrderingApplicator(model, detectRedundantOrdering);
        var queryWithOrderedIncludes = visitor.Visit(query);

        // Analyze the query for ordering and includes in a single pass
        var (hasOrdering, _) = QueryAnalyzer.Analyze(queryWithOrderedIncludes);

        if (hasOrdering)
        {
            if (detectRedundantOrdering)
            {
                RedundantOrder.Validate(query);
            }

            return queryWithOrderedIncludes;
        }

        // The entity type has to come from the source the ordering will be applied to, not from
        // the result type, which may be a projection, a single entity, or a limited page
        var source = GetOrderingSource(queryWithOrderedIncludes);

        var elementType = GetQueryElementType(source.Type);
        if (elementType == null)
        {
            return queryWithOrderedIncludes;
        }

        if (model.FindEntityType(elementType) == null)
        {
            return queryWithOrderedIncludes;
        }

        var configuration = Configuration.TryGet(elementType);
        if (configuration is not { Clauses.Count: > 0 })
        {
            return queryWithOrderedIncludes;
        }

        return ApplyOrdering(queryWithOrderedIncludes, configuration);
    }

    /// <summary>
    /// Operators the default ordering has to be applied beneath rather than after.
    /// </summary>
    /// <remarks>
    /// Skip, Take and the single result operators choose which rows come back, so ordering
    /// them afterwards sorts an arbitrary subset instead of choosing from an ordered one.
    /// Select projects the entity away. EF Core replaces ordering that sits outside an Include
    /// with key ordering. The rest only tag the query and pass their source through unchanged.
    /// </remarks>
    static bool ShouldOrderBefore(MethodCallExpression call)
    {
        var declaringType = call.Method.DeclaringType;
        var name = call.Method.Name;

        if (declaringType == typeof(Queryable))
        {
            return name is
                "Select" or
                "Skip" or
                "Take" or
                "First" or
                "FirstOrDefault" or
                "Last" or
                "LastOrDefault" or
                "ElementAt" or
                "ElementAtOrDefault";
        }

        if (declaringType == typeof(EntityFrameworkQueryableExtensions))
        {
            return name is
                "Include" or
                "ThenInclude" or
                "AsTracking" or
                "AsNoTracking" or
                "AsNoTrackingWithIdentityResolution" or
                "AsSingleQuery" or
                "AsSplitQuery" or
                "IgnoreAutoIncludes" or
                "IgnoreQueryFilters" or
                "TagWith" or
                "TagWithCallSite";
        }

        return false;
    }

    static Expression GetOrderingSource(Expression expression)
    {
        while (expression is MethodCallExpression call &&
               ShouldOrderBefore(call))
        {
            expression = call.Arguments[0];
        }

        return expression;
    }

    static Type? GetQueryElementType(Type type) =>
        queryElementTypeCache.GetOrAdd(type, static type =>
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IQueryable<>) ||
                    genericDef == typeof(IOrderedQueryable<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == typeof(IQueryable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }

            return null;
        });

    static Expression ApplyOrdering(Expression query, Configuration configuration)
    {
        // Push past everything the ordering has to precede, then rebuild the chain around it
        if (query is MethodCallExpression call &&
            ShouldOrderBefore(call))
        {
            var arguments = call.Arguments.ToArray();
            arguments[0] = ApplyOrdering(arguments[0], configuration);
            return call.Update(call.Object, arguments);
        }

        return AppendOrdering(query, configuration);
    }

    static Expression AppendOrdering(Expression source, Configuration configuration)
    {
        var result = source;

        foreach (var clause in configuration.Clauses)
        {
            result = clause.AppendQueryableOrder(result);
        }

        return result;
    }
}
