static class RequiredOrder
{
    static ConcurrentDictionary<Type, bool> validatedContextTypes = new();

    public static void Validate(DbContext context)
    {
        var contextType = context.GetType();

        // Only check and validate once per DbContext type
        if (!validatedContextTypes.TryAdd(contextType, true))
        {
            return;
        }

        // Check if this DbContext requires ordering for all entities (opt-in feature)
        var requireOrdering = context.GetService<IDbContextOptions>()
            .FindExtension<DefaultOrderByOptionsExtension>()
            ?.RequireOrderingForAllEntities ?? false;

        if (requireOrdering)
        {
            ValidateAllEntitiesHaveOrdering(context.Model);
        }
    }

    static void ValidateAllEntitiesHaveOrdering(IModel model)
    {
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
