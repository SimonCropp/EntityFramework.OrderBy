sealed record OrderByClause
{
    internal OrderByClause(Type elementType, ParameterExpression parameter, PropertyInfo[] path, bool descending, bool isThenBy)
    {
        // Pre-build the property access and lambda expression. The path has more than one entry
        // when the ordering reaches through an owned type, for example into a JSON column.
        Expression property = parameter;
        foreach (var segment in path)
        {
            property = Expression.Property(property, segment);
        }

        lambda = Expression.Lambda(property, parameter);

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
        var keyType = path[^1].PropertyType;
        queryableMethod = genericQueryableMethod.MakeGenericMethod(elementType, keyType);
        enumerableMethod = genericEnumerableMethod.MakeGenericMethod(elementType, keyType);
        quotedLambda = Expression.Quote(lambda);
    }

    // The overload taking a comparer cannot be expressed as configuration, so match on the
    // two parameter one. The method table is not held in a field, since keeping it alive for
    // the life of the process buys nothing once these eight have been resolved.
    static MethodInfo FindMethod(Type type, string name) =>
        type.GetMethods()
            .First(_ => _.Name == name &&
                        _.GetParameters().Length == 2);

    static readonly MethodInfo queryableOrderBy = FindMethod(typeof(Queryable), nameof(Queryable.OrderBy));
    static readonly MethodInfo queryableOrderByDescending = FindMethod(typeof(Queryable), nameof(Queryable.OrderByDescending));
    static readonly MethodInfo queryableThenBy = FindMethod(typeof(Queryable), nameof(Queryable.ThenBy));
    static readonly MethodInfo queryableThenByDescending = FindMethod(typeof(Queryable), nameof(Queryable.ThenByDescending));

    static readonly MethodInfo enumerableOrderBy = FindMethod(typeof(Enumerable), nameof(Enumerable.OrderBy));
    static readonly MethodInfo enumerableOrderByDescending = FindMethod(typeof(Enumerable), nameof(Enumerable.OrderByDescending));
    static readonly MethodInfo enumerableThenBy = FindMethod(typeof(Enumerable), nameof(Enumerable.ThenBy));
    static readonly MethodInfo enumerableThenByDescending = FindMethod(typeof(Enumerable), nameof(Enumerable.ThenByDescending));

    readonly LambdaExpression lambda;

    // The fully generic Enumerable method (e.g., OrderBy<ParentEntity, string>)
    // ready to be invoked without further generic type arguments.
    readonly MethodInfo enumerableMethod;

    // The fully generic Queryable method (e.g., OrderBy<ParentEntity, string>)
    // ready to be invoked without further generic type arguments.
    readonly MethodInfo queryableMethod;

    public Expression AppendEnumerableOrder(Expression result) =>
        // Enumerable methods expect Func<T, TKey>, so we pass the lambda directly (no Quote)
        Expression.Call(enumerableMethod, result, lambda);

    // Queryable methods expect Expression<Func<T, TKey>>, so we use Quote()
    readonly UnaryExpression quotedLambda;

    public Expression AppendQueryableOrder(Expression result) =>
        Expression.Call(queryableMethod, result, quotedLambda);
}
