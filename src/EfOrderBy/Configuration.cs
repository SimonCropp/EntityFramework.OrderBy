/// <summary>
/// Stores the default ordering configuration for an entity type.
/// </summary>
sealed class Configuration(Type elementType)
{
    // Keyed by entity CLR type. EF Core transforms the model between finalization and runtime
    // (convention model → RuntimeModel), so we can't key by model object reference.
    static readonly ConcurrentDictionary<Type, Configuration> cache = new();

    internal static void Cache(Type entityType, Configuration configuration)
        => cache.AddOrUpdate(
            entityType,
            configuration,
            (type, existing) =>
            {
                if (existing.ClauseMetadataList.SequenceEqual(configuration.ClauseMetadataList))
                {
                    return existing;
                }

                throw new InvalidOperationException($"Conflicting default ordering configurations for entity type '{type.Name}'. When multiple DbContext types share the same entity, they must configure the same default ordering.");
            });

    internal static Configuration? TryGet(Type entityType)
        => cache.GetValueOrDefault(entityType);

    // Reusable parameter expression for this entity type (e.g., "p" in "p => p.Property").
    // Created once and reused across all clauses for better performance.
    ParameterExpression parameter = Expression.Parameter(elementType, "p");

    internal List<OrderByClause> Clauses { get; } = [];

    /// <summary>
    /// Property names in order, used for creating composite indexes.
    /// </summary>
    internal List<string> PropertyNames { get; } = [];

    internal string? CustomIndexName { get; set; }

    /// <summary>
    /// Whether this configuration was inherited from a base entity type.
    /// Inherited configurations skip index creation (the base type's index covers the same columns).
    /// </summary>
    internal bool IsInherited { get; init; }

    /// <summary>
    /// Metadata for each clause, enabling replay on derived types.
    /// </summary>
    internal List<ClauseMetadata> ClauseMetadataList { get; } = [];

    internal void AddClause(PropertyInfo propertyInfo, bool descending, bool isThenBy)
    {
        Clauses.Add(new(elementType, parameter, propertyInfo, descending, isThenBy));
        PropertyNames.Add(propertyInfo.Name);
        ClauseMetadataList.Add(new(propertyInfo.Name, descending, isThenBy));
    }

    /// <summary>
    /// Creates a new Configuration for a derived type by replaying the clause metadata.
    /// The derived type must have the same properties (inherited from the base).
    /// </summary>
    internal Configuration CreateForDerivedType(Type derivedType)
    {
        var derived = new Configuration(derivedType) { IsInherited = true };
        foreach (var meta in ClauseMetadataList)
        {
            var prop = derivedType.GetProperty(meta.PropertyName);
            if (prop != null)
            {
                derived.AddClause(prop, meta.Descending, meta.IsThenBy);
                continue;
            }

            throw new InvalidOperationException($"Property '{meta.PropertyName}' not found on derived type '{derivedType.Name}'. Cannot inherit ordering from base type '{elementType.Name}'.");
        }

        return derived;
    }

    internal readonly record struct ClauseMetadata(string PropertyName, bool Descending, bool IsThenBy);
}
