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
        return ApplyOrderingBeforeSelect(queryWithOrderedIncludes, elementType, configuration);
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

    static Expression ApplyOrderingBeforeSelect(Expression query, Type elementType, Configuration configuration)
    {
        // Check if the query ends with a Select call
        if (query is MethodCallExpression methodCall &&
            methodCall.Method.DeclaringType == typeof(Queryable) &&
            methodCall.Method.Name == "Select")
        {
            // Apply ordering to the source of the Select, then recreate the Select
            var orderedSource = ApplyOrdering(methodCall.Arguments[0], elementType, configuration);
            return Expression.Call(methodCall.Method, orderedSource, methodCall.Arguments[1]);
        }

        // No Select at the end, apply ordering normally
        return ApplyOrdering(query, elementType, configuration);
    }

    static Expression ApplyOrdering(Expression source, Type elementType, Configuration configuration)
    {
        var result = source;

        foreach (var clause in configuration.Clauses)
        {
            var parameter = Expression.Parameter(elementType, "p");
            var property = Expression.Property(parameter, clause.PropertyInfo);
            var lambda = Expression.Lambda(property, parameter);

            var orderByMethod = clause.QueryableMethod.MakeGenericMethod(elementType, property.Type);

            result = Expression.Call(orderByMethod, result, Expression.Quote(lambda));
        }

        return result;
    }
}
