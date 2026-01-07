/// <summary>
/// Visitor that applies default ordering to nested collections in Include().
/// </summary>
sealed class IncludeOrderingApplicator(IModel model) : ExpressionVisitor
{
    static MethodInfo GetEnumerableMethop(string name) =>
        typeof(Enumerable)
            .GetMethods()
            .First(_ => _.Name == name &&
                        _.GetParameters().Length == 2);

    static readonly MethodInfo enumerableOrderBy = GetEnumerableMethop(nameof(Enumerable.OrderBy));

    static readonly MethodInfo enumerableOrderByDescending = GetEnumerableMethop(nameof(Enumerable.OrderByDescending));

    static readonly MethodInfo enumerableThenBy = GetEnumerableMethop(nameof(Enumerable.ThenBy));

    static readonly MethodInfo enumerableThenByDescending = GetEnumerableMethop(nameof(Enumerable.ThenByDescending));

    static readonly ConcurrentDictionary<Type, Type?> collectionElementTypeCache = new();

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
               method.Name is "Include" or "ThenInclude";
    }

    MethodCallExpression ProcessInclude(MethodCallExpression includeCall)
    {
        // Include has 2 arguments: source and navigation lambda
        if (includeCall.Arguments.Count != 2)
        {
            return includeCall;
        }

        var argument = includeCall.Arguments[1];

        // Check if the navigation lambda returns a collection
        if (argument is UnaryExpression { Operand: LambdaExpression lambda })
        {
            // Check if the navigation already has ordering
            if (Interceptor. HasOrdering(lambda.Body))
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
                    // Build OrderBy expression: _ => _.Employees.OrderBy(...).ThenBy(...)
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

            MethodInfo genericMethod;
            if (clause.IsThenBy)
            {
                genericMethod = clause.Descending ? enumerableThenByDescending : enumerableThenBy;
            }
            else
            {
                genericMethod = clause.Descending ? enumerableOrderByDescending : enumerableOrderBy;
            }

            var orderByMethod = genericMethod.MakeGenericMethod(elementType, property.Type);

            result = Expression.Call(orderByMethod, result, lambda);
        }

        return result;
    }

    static Type? GetCollectionElementType(Type type) =>
        collectionElementTypeCache.GetOrAdd(type, static type =>
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
        });

    Configuration? GetConfiguration(Type elementType)
    {
        var entityType = model.FindEntityType(elementType);
        return entityType?.FindAnnotation(OrderByExtensions.AnnotationName)?.Value as Configuration;
    }
}
