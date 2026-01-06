/// <summary>
/// Interceptor that applies default ordering to queries that don't have explicit OrderBy.
/// </summary>
sealed class Interceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression query, QueryExpressionEventData eventData)
    {
        // First check if there's already ordering in the expression
        if (HasOrdering(query))
        {
            return query;
        }

        // Find the element type of the query from its type
        var elementType = GetQueryElementType(query.Type);
        if (elementType == null)
        {
            return query;
        }

        // Get the configuration from the model
        var entityType = eventData.Context?.Model.FindEntityType(elementType);

        if (entityType?.FindAnnotation(OrderByExtensions.AnnotationName)?.Value is not
                Configuration configuration || configuration.Clauses.Count == 0)
        {
            return query;
        }

        // Apply all ordering clauses
        return ApplyOrdering(query, elementType, configuration);
    }

    static bool HasOrdering(Expression expression)
    {
        var visitor = new OrderingDetector();
        visitor.Visit(expression);
        return visitor.HasOrdering;
    }

    static Type? GetQueryElementType(Type type)
    {
        // Check if type itself is IQueryable<T>
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(IQueryable<>) || genericDef == typeof(IOrderedQueryable<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        // Check interfaces
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IQueryable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    static Expression ApplyOrdering(Expression source, Type elementType, Configuration configuration)
    {
        var result = source;

        foreach (var clause in configuration.Clauses)
        {
            var parameter = Expression.Parameter(elementType, "x");
            var property = Expression.Property(parameter, clause.Property);
            var lambda = Expression.Lambda(property, parameter);

            string methodName;
            if (clause.IsThenBy)
            {
                methodName = clause.Descending ? "ThenByDescending" : "ThenBy";
            }
            else
            {
                methodName = clause.Descending ? "OrderByDescending" : "OrderBy";
            }

            var orderByMethod = typeof(Queryable)
                .GetMethods()
                .First(_ => _.Name == methodName && _.GetParameters().Length == 2)
                .MakeGenericMethod(elementType, property.Type);

            result = Expression.Call(orderByMethod, result, Expression.Quote(lambda));
        }

        return result;
    }

    sealed class OrderingDetector : ExpressionVisitor
    {
        public bool HasOrdering { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if (method.DeclaringType == typeof(Queryable) &&
                method.Name is
                    "OrderBy" or
                    "OrderByDescending" or
                    "ThenBy" or
                    "ThenByDescending")
            {
                HasOrdering = true;
            }
            return base.VisitMethodCall(node);
        }
    }
}
