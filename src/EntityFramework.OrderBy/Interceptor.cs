/// <summary>
/// Interceptor that applies default ordering to queries that don't have explicit OrderBy.
/// </summary>
sealed class Interceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression query, QueryExpressionEventData eventData)
    {
        if (eventData.Context == null)
        {
            return query;
        }

        var model = eventData.Context.Model;

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
        return ApplyOrdering(queryWithOrderedIncludes, elementType, configuration);
    }

    static bool HasOrdering(Expression expression)
    {
        var visitor = new OrderingDetector();
        visitor.Visit(expression);
        return visitor.HasOrdering;
    }

    static Type? GetQueryElementType(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(IQueryable<>) || genericDef == typeof(IOrderedQueryable<>))
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

    /// <summary>
    /// Visitor that applies default ordering to nested collections in Include().
    /// </summary>
    sealed class IncludeOrderingApplicator(IModel model) : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // First, recursively visit children
            var visited = (MethodCallExpression)base.VisitMethodCall(node);

            // Check if this is an Include method call
            if (IsIncludeMethod(visited))
            {
                return ProcessInclude(visited);
            }

            return visited;
        }

        static bool IsIncludeMethod(MethodCallExpression node)
        {
            var method = node.Method;
            return method.DeclaringType?.Name == "EntityFrameworkQueryableExtensions" &&
                   (method.Name == "Include" || method.Name == "ThenInclude");
        }

        Expression ProcessInclude(MethodCallExpression includeCall)
        {
            // Include has 2 arguments: source and navigation lambda
            if (includeCall.Arguments.Count != 2)
            {
                return includeCall;
            }

            var navigationArg = includeCall.Arguments[1];

            // Check if the navigation lambda returns a collection
            if (navigationArg is UnaryExpression { Operand: LambdaExpression lambda })
            {
                // Check if the navigation already has ordering
                if (HasOrdering(lambda.Body))
                {
                    // Already has explicit ordering, don't apply default
                    return includeCall;
                }

                // Get the element type of the collection
                var collectionType = lambda.Body.Type;
                var elementType = GetCollectionElementType(collectionType);

                if (elementType != null)
                {
                    var configuration = GetConfiguration(elementType);
                    if (configuration is { Clauses.Count: > 0 })
                    {
                        // Build OrderBy expression:  d => d.Employees.OrderBy(...).ThenBy(...)
                        var orderedNavigation = BuildOrderedNavigationExpression(lambda.Body, elementType, configuration);
                        var orderedLambda = Expression.Lambda(orderedNavigation, lambda.Parameters);

                        // Get the Include method with the new return type
                        // Original: Include<Department, List<Employee>>(...)
                        // New: Include<Department, IOrderedEnumerable<Employee>>(...)
                        var sourceType = includeCall.Method.GetGenericArguments()[0]; // TEntity
                        var newPropertyType = orderedNavigation.Type; // IOrderedEnumerable<Employee>

                        var includeMethod = includeCall.Method.GetGenericMethodDefinition()
                            .MakeGenericMethod(sourceType, newPropertyType);

                        // Recreate the Include call with the new method and ordered lambda
                        return Expression.Call(
                            includeMethod,
                            includeCall.Arguments[0],
                            Expression.Quote(orderedLambda));
                    }
                }
            }

            return includeCall;
        }

        static Expression BuildOrderedNavigationExpression(Expression navigationProperty, Type elementType, Configuration configuration)
        {
            // Use Enumerable methods (not Queryable) because navigation properties are IEnumerable<T>
            // EF Core's ExtractIncludeFilter will handle these specially
            var result = navigationProperty;

            // Apply each ordering clause using Enumerable methods
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

                var orderByMethod = typeof(Enumerable)
                    .GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(elementType, property.Type);

                result = Expression.Call(orderByMethod, result, lambda);
            }

            return result;
        }

        static Type? GetCollectionElementType(Type type)
        {
            // Check for IEnumerable<T>
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(List<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            // Check interfaces
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }

            return null;
        }

        Configuration? GetConfiguration(Type elementType)
        {
            var entityType = model.FindEntityType(elementType);
            return entityType?.FindAnnotation(OrderByExtensions.AnnotationName)?.Value as Configuration;
        }
    }

    sealed class OrderingDetector : ExpressionVisitor
    {
        public bool HasOrdering { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;
            if ((method.DeclaringType == typeof(Queryable) || method.DeclaringType == typeof(Enumerable)) &&
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
