sealed record OrderByClause
{
    internal OrderByClause(Type elementType, PropertyInfo propertyInfo, bool descending, bool isThenBy)
    {
        PropertyInfo = propertyInfo;

        MethodInfo genericQueryableMethod;
        MethodInfo genericEnumerableMethod;

        if (isThenBy)
        {
            genericQueryableMethod = descending ? queryableThenByDescending : queryableThenBy;
            genericEnumerableMethod = descending ? enumerableThenByDescending : enumerableThenBy;
        }
        else
        {
            genericQueryableMethod = descending ? queryableOrderByDescending : queryableOrderBy;
            genericEnumerableMethod = descending ? enumerableOrderByDescending : enumerableOrderBy;
        }

        // Pre-compute the fully generic methods (e.g., OrderBy<ParentEntity, string>)
        QueryableMethod = genericQueryableMethod.MakeGenericMethod(elementType, propertyInfo.PropertyType);
        EnumerableMethod = genericEnumerableMethod.MakeGenericMethod(elementType, propertyInfo.PropertyType);
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
    /// The fully generic Queryable method (e.g., OrderBy&lt;ParentEntity, string&gt;)
    /// ready to be invoked without further generic type arguments.
    /// </summary>
    public MethodInfo QueryableMethod { get; }

    /// <summary>
    /// The fully generic Enumerable method (e.g., OrderBy&lt;ParentEntity, string&gt;)
    /// ready to be invoked without further generic type arguments.
    /// </summary>
    public MethodInfo EnumerableMethod { get; }

    public PropertyInfo PropertyInfo { get; }
}
