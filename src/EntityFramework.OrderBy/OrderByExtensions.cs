namespace EntityFrameworkOrderBy;

/// <summary>
/// Provides extension methods for configuring default ordering on Entity Framework Core queries.
/// </summary>
public static class OrderByExtensions
{
    internal const string AnnotationName = "DefaultOrderBy:Configuration";
    static Interceptor interceptor = new();

    /// <summary>
    /// Adds the default ordering interceptor to automatically apply ordering to queries
    /// based on fluent configuration.
    /// </summary>
    /// <param name="builder">The DbContextOptionsBuilder to configure.</param>
    /// <param name="requireOrderingForAllEntities">
    /// When true, throws an exception during the first query if any entity type
    /// in the model doesn't have default ordering configured. Validation occurs
    /// once per DbContext type.
    /// </param>
    public static DbContextOptionsBuilder UseDefaultOrderBy(
        this DbContextOptionsBuilder builder,
        bool requireOrderingForAllEntities = false)
    {
        builder.AddInterceptors(interceptor);

        if (requireOrderingForAllEntities)
        {
            // Store the requirement in the builder's options
            // We'll use a marker extension to track this
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(
                new DefaultOrderByOptionsExtension(requireOrderingForAllEntities));
        }

        return builder;
    }

    /// <summary>
    /// Configures a default ascending ordering for this entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <typeparam name="TProperty">The type of the property to order by.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="property">A lambda expression representing the property to order by.</param>
    /// <returns>An <see cref="OrderByBuilder{TEntity}"/> for chaining additional ordering operations.</returns>
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
    /// <typeparam name="TEntity">The entity type being configured.</typeparam>
    /// <typeparam name="TProperty">The type of the property to order by.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="property">A lambda expression representing the property to order by.</param>
    /// <returns>An <see cref="OrderByBuilder{TEntity}"/> for chaining additional ordering operations.</returns>
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
