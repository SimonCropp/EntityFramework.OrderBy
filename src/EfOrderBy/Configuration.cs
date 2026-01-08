/// <summary>
/// Stores the default ordering configuration for an entity type.
/// </summary>
sealed class Configuration(Type elementType)
{
    // Reusable parameter expression for this entity type (e.g., "p" in "p => p.Property").
    // Created once and reused across all clauses for better performance.
    ParameterExpression parameter = Expression.Parameter(elementType, "p");

    internal List<OrderByClause> Clauses { get; } = [];

    /// <summary>
    /// Property names in order, used for creating composite indexes.
    /// </summary>
    internal List<string> PropertyNames { get; } = [];

    internal void AddClause(PropertyInfo propertyInfo, bool descending, bool isThenBy)
    {
        Clauses.Add(new(elementType, parameter, propertyInfo, descending, isThenBy));
        PropertyNames.Add(propertyInfo.Name);
    }
}
