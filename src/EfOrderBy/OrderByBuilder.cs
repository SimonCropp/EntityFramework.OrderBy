namespace EfOrderBy;

/// <summary>
/// Builder for configuring default ordering on an entity type.
/// </summary>
public sealed class OrderByBuilder<TEntity>
    where TEntity : class
{
    Configuration configuration;
    EntityTypeBuilder<TEntity> entityBuilder;

    internal OrderByBuilder(EntityTypeBuilder<TEntity> builder, PropertyInfo propertyInfo, bool descending)
    {
        entityBuilder = builder;
        configuration = new(typeof(TEntity));
        configuration.AddClause(propertyInfo, descending, isThenBy: false);

        // Store configuration in model annotation
        builder.Metadata.SetAnnotation(OrderByExtensions.AnnotationName, configuration);

        UpdateIndex();
    }

    /// <summary>
    /// Adds a secondary ascending ordering.
    /// </summary>
    public OrderByBuilder<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var propertyInfo = GetPropertyInfo(property);
        configuration.AddClause(propertyInfo, descending: false, isThenBy: true);
        UpdateIndex();
        return this;
    }

    /// <summary>
    /// Adds a secondary descending ordering.
    /// </summary>
    public OrderByBuilder<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var propertyInfo = GetPropertyInfo(property);
        configuration.AddClause(propertyInfo, descending: true, isThenBy: true);
        UpdateIndex();
        return this;
    }

    /// <summary>
    /// Creates or updates a composite index for all ordering properties.
    /// </summary>
    void UpdateIndex()
    {
        var indexName = $"IX_{typeof(TEntity).Name}_DefaultOrder";
        var entityType = entityBuilder.Metadata;

        // Remove existing index with this name (if any) before creating the updated one
        var existingIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == indexName);
        if (existingIndex != null)
        {
            entityType.RemoveIndex(existingIndex);
        }

        entityBuilder
            .HasIndex(configuration.PropertyNames.ToArray())
            .HasDatabaseName(indexName);
    }

    static PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        if (property.Body is MemberExpression { Member: PropertyInfo propertyInfo })
        {
            return propertyInfo;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(property));
    }
}
