namespace EfOrderBy;

/// <summary>
/// Builder for configuring default ordering on an entity type.
/// </summary>
public sealed class OrderByBuilder<TEntity>
    where TEntity : class
{
    Configuration configuration;

    internal OrderByBuilder(EntityTypeBuilder<TEntity> builder, PropertyInfo propertyInfo, bool descending)
    {
        configuration = new(typeof(TEntity));
        configuration.AddClause(propertyInfo, descending, isThenBy: false);

        // Store configuration in model annotation
        builder.Metadata.SetAnnotation(OrderByExtensions.AnnotationName, configuration);
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

    static PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        if (property.Body is MemberExpression { Member: PropertyInfo propertyInfo })
        {
            return propertyInfo;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(property));
    }
}
