namespace EntityFramework.OrderBy;

/// <summary>
/// Interceptor that applies default ordering to queries that don't have explicit OrderBy.
/// </summary>
public sealed class DefaultOrderByInterceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        // First check if there's already ordering in the expression
        if (HasOrdering(queryExpression))
        {
            return queryExpression;
        }

        // Find the element type of the query from its type
        var elementType = GetQueryElementType(queryExpression.Type);
        if (elementType == null)
        {
            return queryExpression;
        }

        // Get the configuration from the model
        var entityType = eventData.Context?.Model.FindEntityType(elementType);
        var configuration = entityType?.FindAnnotation(DefaultOrderByExtensions.AnnotationName)?.Value as DefaultOrderByConfiguration;

        if (configuration == null || configuration.Clauses.Count == 0)
        {
            return queryExpression;
        }

        // Apply all ordering clauses
        return ApplyOrdering(queryExpression, elementType, configuration);
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

    static Expression ApplyOrdering(Expression source, Type elementType, DefaultOrderByConfiguration configuration)
    {
        var result = source;

        foreach (var clause in configuration.Clauses)
        {
            var parameter = Expression.Parameter(elementType, "x");
            var property = Expression.Property(parameter, clause.PropertyName);
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
            if (node.Method.DeclaringType == typeof(Queryable) &&
                node.Method.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
            {
                HasOrdering = true;
            }
            return base.VisitMethodCall(node);
        }
    }
}
