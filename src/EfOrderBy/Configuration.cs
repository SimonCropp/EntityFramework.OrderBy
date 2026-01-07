/// <summary>
/// Stores the default ordering configuration for an entity type.
/// </summary>
sealed class Configuration
{
    internal List<OrderByClause> Clauses { get; } = [];

    internal void AddClause(PropertyInfo propertyInfo, bool descending, bool isThenBy) =>
        Clauses.Add(new(propertyInfo, descending, isThenBy));
}