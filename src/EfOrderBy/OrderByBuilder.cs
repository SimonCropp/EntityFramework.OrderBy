namespace EfOrderBy;

/// <summary>
/// Builder for configuring default ordering on an entity type.
/// </summary>
public sealed class OrderByBuilder<TEntity>
    where TEntity : class
{
    const int maxIndexNameLength = 128;

    Configuration configuration;
    IMutableModel model;

    internal OrderByBuilder(EntityTypeBuilder<TEntity> builder, PropertyInfo propertyInfo, bool descending)
    {
        model = builder.Metadata.Model;
        configuration = new(typeof(TEntity));
        configuration.AddClause(propertyInfo, descending, isThenBy: false);

        builder.Metadata.SetOrderByConfiguration(configuration);
    }

    /// <summary>
    /// Adds a secondary ascending ordering.
    /// </summary>
    public OrderByBuilder<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var propertyInfo = GetPropertyInfo(property);
        configuration.AddClause(propertyInfo, descending: false, isThenBy: true);
        return this;
    }

    /// <summary>
    /// Adds a secondary descending ordering.
    /// </summary>
    public OrderByBuilder<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var propertyInfo = GetPropertyInfo(property);
        configuration.AddClause(propertyInfo, descending: true, isThenBy: true);
        return this;
    }

    /// <summary>
    /// Specifies a custom index name for the default ordering index.
    /// Use this when the auto-generated index name would exceed the 128 character limit.
    /// </summary>
    public OrderByBuilder<TEntity> WithIndexName(string indexName)
    {
        if (model.IsIndexCreationDisabled())
        {
            throw new("WithIndexName() cannot be used when index creation is disabled. Remove the createIndexes: false option from UseDefaultOrderBy() or remove the WithIndexName() call.");
        }

        if (string.IsNullOrWhiteSpace(indexName))
        {
            throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));
        }

        if (indexName.Length > maxIndexNameLength)
        {
            throw new ArgumentException($"Index name '{indexName}' exceeds maximum length of {maxIndexNameLength} characters.", nameof(indexName));
        }

        configuration.CustomIndexName = indexName;
        return this;
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
