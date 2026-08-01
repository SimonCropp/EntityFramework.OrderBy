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

                throw new($"Conflicting default ordering configurations for entity type '{type.Name}'. When multiple DbContext types share the same entity, they must configure the same default ordering.");
            });

    internal static Configuration? TryGet(Type entityType)
        => cache.GetValueOrDefault(entityType);

    // Reusable parameter expression for this entity type (e.g., "p" in "p => p.Property").
    // Created once and reused across all clauses for better performance.
    ParameterExpression parameter = Expression.Parameter(elementType, "p");

    internal List<OrderByClause> Clauses { get; } = [];

    /// <summary>
    /// Property names in order, used for creating composite indexes.
    /// Only meaningful when <see cref="HasNestedPath" /> is false.
    /// </summary>
    internal List<string> PropertyNames { get; } = [];

    /// <summary>
    /// Whether any clause orders by a property reached through an owned type, for example a
    /// property of a JSON mapped column. Those are not columns of the entity's own table, so
    /// they cannot be named in an index.
    /// </summary>
    internal bool HasNestedPath { get; private set; }

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

    internal void AddClause(PropertyInfo[] path, bool descending, bool isThenBy)
    {
        Clauses.Add(new(elementType, parameter, path, descending, isThenBy));

        if (path.Length > 1)
        {
            HasNestedPath = true;
        }

        PropertyNames.Add(path[0].Name);
        ClauseMetadataList.Add(new(PropertyPath.Describe(path), descending, isThenBy));
    }

    /// <summary>
    /// Creates a new Configuration for a derived type by replaying the clause metadata.
    /// The derived type must have the same properties (inherited from the base).
    /// </summary>
    // The default lookup omits non public properties, which an entity can map,
    // so without these the ordering fails to inherit for those properties.
    const BindingFlags propertyFlags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance;

    internal Configuration CreateForDerivedType(Type derivedType)
    {
        var derived = new Configuration(derivedType) { IsInherited = true };
        foreach (var meta in ClauseMetadataList)
        {
            derived.AddClause(ResolvePath(derivedType, meta.PropertyPath), meta.Descending, meta.IsThenBy);
        }

        return derived;
    }

    // Only the first segment is declared by the entity, so only it changes on a derived type.
    // The rest hang off the owned types the path reaches through and resolve the same way.
    PropertyInfo[] ResolvePath(Type derivedType, string path)
    {
        var names = path.Split('.');
        var resolved = new PropertyInfo[names.Length];
        var declaring = derivedType;

        for (var index = 0; index < names.Length; index++)
        {
            var property = declaring.GetProperty(names[index], propertyFlags);
            if (property == null)
            {
                throw new($"Property '{path}' not found on derived type '{derivedType.Name}'. Cannot inherit ordering from base type '{elementType.Name}'.");
            }

            resolved[index] = property;
            declaring = property.PropertyType;
        }

        return resolved;
    }

    /// <summary>
    /// A clause as configured, with <paramref name="PropertyPath" /> dotted when the ordering
    /// reaches through an owned type, for example "Metadata.Rank" for a JSON mapped column.
    /// </summary>
    internal readonly record struct ClauseMetadata(string PropertyPath, bool Descending, bool IsThenBy);
}
