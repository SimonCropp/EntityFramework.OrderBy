namespace EntityFramework.OrderBy;

/// <summary>
/// Stores the default ordering configuration for an entity type.
/// </summary>
public sealed class DefaultOrderByConfiguration
{
    internal List<OrderByClause> Clauses { get; } = [];

    internal void AddClause(string propertyName, bool descending, bool isThenBy)
    {
        Clauses.Add(new(propertyName, descending, isThenBy));
    }
}

sealed record OrderByClause(string PropertyName, bool Descending, bool IsThenBy);
