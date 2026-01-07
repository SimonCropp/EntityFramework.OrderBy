/// <summary>
/// Stores the default ordering configuration for an entity type.
/// </summary>
sealed class Configuration(Type elementType)
{
    public Type ElementType { get; } = elementType;

    /// <summary>
    /// Reusable parameter expression for this entity type (e.g., "p" in "p => p.Property").
    /// Created once and reused across all clauses for better performance.
    /// </summary>
    public ParameterExpression Parameter { get; } = Expression.Parameter(elementType, "p");

    internal List<OrderByClause> Clauses { get; } = [];

    internal void AddClause(PropertyInfo propertyInfo, bool descending, bool isThenBy) =>
        Clauses.Add(new(ElementType, Parameter, propertyInfo, descending, isThenBy));
}
