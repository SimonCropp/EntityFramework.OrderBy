/// <summary>
/// Interceptor that applies default ordering to queries that don't have explicit OrderBy.
/// </summary>
sealed class Interceptor : IQueryExpressionInterceptor
{
    readonly ConcurrentDictionary<Type, bool> validatedContextTypes = new();

    public Expression QueryCompilationStarting(Expression query, QueryExpressionEventData eventData)
    {
        if (eventData.Context == null)
        {
            return query;
        }

        var model = eventData.Context.Model;

        // Check if this DbContext requires ordering for all entities (opt-in feature)
        var requireOrdering = eventData.Context.GetService<IDbContextOptions>()
            .FindExtension<DefaultOrderByOptionsExtension>()
            ?.RequireOrderingForAllEntities ?? false;

        if (requireOrdering)
        {
            ValidateAllEntitiesHaveOrdering(eventData.Context.GetType(), model);
        }

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

    public static bool HasOrdering(Expression expression)
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

    void ValidateAllEntitiesHaveOrdering(Type contextType, IModel model)
    {
        // Only validate once per DbContext type
        if (!validatedContextTypes.TryAdd(contextType, true))
        {
            return;
        }

        var entitiesWithoutOrdering = new List<string>();

        foreach (var entity in model.GetEntityTypes())
        {
            // Skip entity types that are not queryable (owned types, etc.)
            if (entity.IsOwned() || entity.HasSharedClrType)
            {
                continue;
            }

            // Check if this entity type has default ordering configured
            var hasOrdering = entity.FindAnnotation(OrderByExtensions.AnnotationName)?.Value
                is Configuration { Clauses.Count: > 0 };

            if (!hasOrdering)
            {
                entitiesWithoutOrdering.Add(entity.ClrType.Name);
            }
        }

        if (entitiesWithoutOrdering.Count > 0)
        {
            throw new($"Default ordering is required for all entity types but the following entities do not have ordering configured: {string.Join(", ", entitiesWithoutOrdering)}. Use modelBuilder.Entity<T>().OrderBy() to configure default ordering.");
        }
    }
}
