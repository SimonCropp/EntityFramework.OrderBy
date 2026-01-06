namespace EntityFrameworkOrderBy;

public static class OrderByExtensions
{
    internal const string AnnotationName = "DefaultOrderBy:Configuration";
    static Interceptor interceptor = new();

    /// <summary>
    /// Adds the default ordering interceptor to automatically apply ordering to queries
    /// based on fluent configuration.
    /// </summary>
    public static DbContextOptionsBuilder UseDefaultOrderBy(this DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(interceptor);
        return builder;
    }

    /// <summary>
    /// Configures a default ascending ordering for this entity type.
    /// </summary>
    public static OrderByBuilder<TEntity> OrderBy<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> property)
        where TEntity : class
    {
        var name = GetPropertyName(property);
        return new(builder, name, descending: false);
    }

    /// <summary>
    /// Configures a default descending ordering for this entity type.
    /// </summary>
    public static OrderByBuilder<TEntity> OrderByDescending<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> property)
        where TEntity : class
    {
        var name = GetPropertyName(property);
        return new(builder, name, descending: true);
    }

    static string GetPropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        if (property.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(property));
    }
}
