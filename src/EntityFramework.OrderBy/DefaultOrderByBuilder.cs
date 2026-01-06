namespace EntityFramework.OrderBy;

/// <summary>
/// Builder for configuring default ordering on an entity type.
/// </summary>
public sealed class DefaultOrderByBuilder<TEntity>
    where TEntity : class
{
    DefaultOrderByConfiguration configuration;

    internal DefaultOrderByBuilder(EntityTypeBuilder<TEntity> entityTypeBuilder, string propertyName, bool descending)
    {
        configuration = new();
        configuration.AddClause(propertyName, descending, isThenBy: false);

        // Store configuration in model annotation
        entityTypeBuilder.Metadata.SetAnnotation(DefaultOrderByExtensions.AnnotationName, configuration);
    }

    /// <summary>
    /// Adds a secondary ascending ordering.
    /// </summary>
    public DefaultOrderByBuilder<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        var name = GetPropertyName(property);
        configuration.AddClause(name, descending: false, isThenBy: true);
        return this;
    }

    /// <summary>
    /// Adds a secondary descending ordering.
    /// </summary>
    public DefaultOrderByBuilder<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> property)
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
