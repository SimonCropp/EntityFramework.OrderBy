sealed record OrderByClause
{
    public OrderByClause(PropertyInfo PropertyInfo, bool descending, bool isThenBy)
    {
        this.PropertyInfo = PropertyInfo;
        if (isThenBy)
        {
            QueryableMethod = descending ? queryableThenByDescending : queryableThenBy;
            EnumerableMethod = descending ? enumerableThenByDescending : enumerableThenBy;
        }
        else
        {
            QueryableMethod = descending ? queryableOrderByDescending : queryableOrderBy;
            EnumerableMethod = descending ? enumerableOrderByDescending : enumerableOrderBy;
        }
    }

    static MethodInfo GetQueryableMethod(string name) =>
        typeof(Queryable)
            .GetMethods()
            .First(_ => _.Name == name &&
                        _.GetParameters().Length == 2);

    static MethodInfo GetEnumerableMethod(string name) =>
        typeof(Enumerable)
            .GetMethods()
            .First(_ => _.Name == name &&
                        _.GetParameters().Length == 2);

    static readonly MethodInfo queryableOrderBy = GetQueryableMethod(nameof(Queryable.OrderBy));
    static readonly MethodInfo queryableOrderByDescending = GetQueryableMethod(nameof(Queryable.OrderByDescending));
    static readonly MethodInfo queryableThenBy = GetQueryableMethod(nameof(Queryable.ThenBy));
    static readonly MethodInfo queryableThenByDescending = GetQueryableMethod(nameof(Queryable.ThenByDescending));

    static readonly MethodInfo enumerableOrderBy = GetEnumerableMethod(nameof(Enumerable.OrderBy));
    static readonly MethodInfo enumerableOrderByDescending = GetEnumerableMethod(nameof(Enumerable.OrderByDescending));
    static readonly MethodInfo enumerableThenBy = GetEnumerableMethod(nameof(Enumerable.ThenBy));
    static readonly MethodInfo enumerableThenByDescending = GetEnumerableMethod(nameof(Enumerable.ThenByDescending));

    /// <summary>
    /// The appropriate Queryable method (OrderBy, OrderByDescending, ThenBy, or ThenByDescending)
    /// for this clause.
    /// </summary>
    public MethodInfo QueryableMethod { get; }

    /// <summary>
    /// The appropriate Enumerable method (OrderBy, OrderByDescending, ThenBy, or ThenByDescending)
    /// for this clause.
    /// </summary>
    public MethodInfo EnumerableMethod { get; }

    public PropertyInfo PropertyInfo { get; init; }
}
