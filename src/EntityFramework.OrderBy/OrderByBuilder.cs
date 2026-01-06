namespace EntityFrameworkOrderBy;

/// <summary>
/// Builder for configuring default ordering on an entity type.
/// </summary>
public sealed class OrderByBuilder<TEntity>
    where TEntity : class
{
    Configuration configuration;

    internal OrderByBuilder(EntityTypeBuilder<TEntity> entityTypeBuilder, string propertyName, bool descending)
    {
        configuration = new();
        configuration.AddClause(propertyName, descending, isThenBy: false);

        // Store configuration in model annotation
        entityTypeBuilder.Metadata.SetAnnotation(OrderByExtensions.AnnotationName, configuration);
    }

    /// <summary>
    /// Adds a secondary ascending ordering.
    /// </summary>
    public OrderByBuilder<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var name = GetPropertyName(property);
        configuration.AddClause(name, descending: false, isThenBy: true);
        return this;
    }

    /// <summary>
    /// Adds a secondary descending ordering.
    /// </summary>
    public OrderByBuilder<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var name = GetPropertyName(property);
        configuration.AddClause(name, descending: true, isThenBy: true);
        return this;
    }

    static string GetPropertyName<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        if (property.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(property));
    }
}
