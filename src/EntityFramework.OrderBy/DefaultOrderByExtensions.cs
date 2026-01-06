using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFramework.OrderBy;

public static class DefaultOrderByExtensions
{
    internal const string AnnotationName = "DefaultOrderBy:Configuration";

    /// <summary>
    /// Adds the default ordering interceptor to automatically apply ordering to queries
    /// based on fluent configuration.
    /// </summary>
    public static DbContextOptionsBuilder UseDefaultOrderBy(this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new DefaultOrderByInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Configures a default ascending ordering for this entity type.
    /// </summary>
    public static DefaultOrderByBuilder<TEntity> HasDefaultOrderBy<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class
    {
        var propertyName = GetPropertyName(propertyExpression);
        return new DefaultOrderByBuilder<TEntity>(builder, propertyName, descending: false);
    }

    /// <summary>
    /// Configures a default descending ordering for this entity type.
    /// </summary>
    public static DefaultOrderByBuilder<TEntity> HasDefaultOrderByDescending<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class
    {
        var propertyName = GetPropertyName(propertyExpression);
        return new DefaultOrderByBuilder<TEntity>(builder, propertyName, descending: true);
    }

    static string GetPropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(propertyExpression));
    }
}
