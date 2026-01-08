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

        // First, process Include nodes to add ordering to nested collections
        var visitor = new IncludeOrderingApplicator(model);
        var queryWithOrderedIncludes = visitor.Visit(query);

        // Then, check if the top-level query needs default ordering
        if (HasOrdering(queryWithOrderedIncludes))
        {
            return queryWithOrderedIncludes;
        }

        var elementType = GetQueryElementType(queryWithOrderedIncludes.Type);
        if (elementType == null)
        {
            return queryWithOrderedIncludes;
        }

        var entityType = model.FindEntityType(elementType);
        if (entityType?.FindAnnotation(OrderByExtensions.AnnotationName)?.Value is not
                Configuration configuration || configuration.Clauses.Count == 0)
        {
            return queryWithOrderedIncludes;
        }

        // Apply default ordering to the top-level query
        // If there's a Select projection at the end, we need to insert OrderBy before it
        return ApplyOrderingBeforeSelect(queryWithOrderedIncludes, configuration);
    }

    public static bool HasOrdering(Expression expression)
    {
        var visitor = new OrderingDetector();
        visitor.Visit(expression);
        return visitor.HasOrdering;
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

    static Expression ApplyOrderingBeforeSelect(Expression query, Configuration configuration)
    {
        // Check if the query ends with a Select call
        if (query is MethodCallExpression methodCall &&
            methodCall.Method.DeclaringType == typeof(Queryable) &&
            methodCall.Method.Name == "Select")
        {
            // Apply ordering to the source of the Select, then recreate the Select
            var orderedSource = ApplyOrdering(methodCall.Arguments[0], configuration);
            return Expression.Call(methodCall.Method, orderedSource, methodCall.Arguments[1]);
        }

        // Check if the query contains Include - need to apply ordering before Include
        // to prevent EF Core from replacing it with just Id ordering
        if (ContainsInclude(query))
        {
            return ApplyOrderingBeforeInclude(query, configuration);
        }

        // No Select or Include at the end, apply ordering normally
        return ApplyOrdering(query, configuration);
    }

    static bool ContainsInclude(Expression expression)
    {
        var visitor = new IncludeDetector();
        visitor.Visit(expression);
        return visitor.HasInclude;
    }

    static Expression ApplyOrderingBeforeInclude(Expression query, Configuration configuration)
    {
        // Find the last method call before Include
        if (query is MethodCallExpression methodCall &&
            methodCall.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
            (methodCall.Method.Name == "Include" || methodCall.Method.Name == "ThenInclude"))
        {
            // Apply ordering to the source of the Include, then recreate the Include
            var orderedSource = ApplyOrderingBeforeInclude(methodCall.Arguments[0], configuration);

            // Recreate the Include call with the ordered source
            var args = new Expression[methodCall.Arguments.Count];
            args[0] = orderedSource;
            for (var i = 1; i < methodCall.Arguments.Count; i++)
            {
                args[i] = methodCall.Arguments[i];
            }

            return Expression.Call(methodCall.Method, args);
        }

        // No Include at this level, apply ordering here
        return ApplyOrdering(query, configuration);
    }

    sealed class IncludeDetector : ExpressionVisitor
    {
        public bool HasInclude { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
                (node.Method.Name == "Include" || node.Method.Name == "ThenInclude"))
            {
                HasInclude = true;
            }
            return base.VisitMethodCall(node);
        }
    }

    static Expression ApplyOrdering(Expression source, Configuration configuration)
    {
        var result = source;

        foreach (var clause in configuration.Clauses)
        {
            result = clause.AppendQueryableOrder(result);
        }

        return result;
    }
}
