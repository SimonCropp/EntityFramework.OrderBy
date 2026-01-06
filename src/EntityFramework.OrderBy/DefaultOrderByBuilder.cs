using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFramework.OrderBy;

/// <summary>
/// Builder for configuring default ordering on an entity type.
/// </summary>
public sealed class DefaultOrderByBuilder<TEntity> where TEntity : class
{
    readonly EntityTypeBuilder<TEntity> _entityTypeBuilder;
    readonly DefaultOrderByConfiguration _configuration;

    internal DefaultOrderByBuilder(EntityTypeBuilder<TEntity> entityTypeBuilder, string propertyName, bool descending)
    {
        _entityTypeBuilder = entityTypeBuilder;
        _configuration = new DefaultOrderByConfiguration();
        _configuration.AddClause(propertyName, descending, isThenBy: false);

        // Store configuration in model annotation
        _entityTypeBuilder.Metadata.SetAnnotation(DefaultOrderByExtensions.AnnotationName, _configuration);
    }

    /// <summary>
    /// Adds a secondary ascending ordering.
    /// </summary>
    public DefaultOrderByBuilder<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _configuration.AddClause(propertyName, descending: false, isThenBy: true);
        return this;
    }

    /// <summary>
    /// Adds a secondary descending ordering.
    /// </summary>
    public DefaultOrderByBuilder<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        _configuration.AddClause(propertyName, descending: true, isThenBy: true);
        return this;
    }

    static string GetPropertyName<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(propertyExpression));
    }
}
