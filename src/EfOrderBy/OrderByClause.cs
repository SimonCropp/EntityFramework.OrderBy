sealed record OrderByClause
{
    internal OrderByClause(Type elementType, ParameterExpression parameter, PropertyInfo propertyInfo, bool descending, bool isThenBy)
    {
        // Pre-build the property access and lambda expression
        var property = Expression.Property(parameter, propertyInfo);
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
        queryableMethod = genericQueryableMethod.MakeGenericMethod(elementType, propertyInfo.PropertyType);
        enumerableMethod = genericEnumerableMethod.MakeGenericMethod(elementType, propertyInfo.PropertyType);
        quotedLambda = Expression.Quote(lambda);
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

    static MethodInfo queryableOrderBy = GetQueryableMethod(nameof(Queryable.OrderBy));
    static MethodInfo queryableOrderByDescending = GetQueryableMethod(nameof(Queryable.OrderByDescending));
    static MethodInfo queryableThenBy = GetQueryableMethod(nameof(Queryable.ThenBy));
    static MethodInfo queryableThenByDescending = GetQueryableMethod(nameof(Queryable.ThenByDescending));

    static MethodInfo enumerableOrderBy = GetEnumerableMethod(nameof(Enumerable.OrderBy));
    static MethodInfo enumerableOrderByDescending = GetEnumerableMethod(nameof(Enumerable.OrderByDescending));
    static MethodInfo enumerableThenBy = GetEnumerableMethod(nameof(Enumerable.ThenBy));
    static MethodInfo enumerableThenByDescending = GetEnumerableMethod(nameof(Enumerable.ThenByDescending));
    LambdaExpression lambda;

    // The fully generic Enumerable method (e.g., OrderBy&lt;ParentEntity, string&gt;)
    // ready to be invoked without further generic type arguments.
    MethodInfo enumerableMethod;

    // The fully generic Queryable method (e.g., OrderBy&lt;ParentEntity, string&gt;)
    // ready to be invoked without further generic type arguments.
    MethodInfo queryableMethod;

    public Expression AppendEnumerableOrder(Expression result) =>
        // Enumerable methods expect Func<T, TKey>, so we pass the lambda directly (no Quote)
        Expression.Call(enumerableMethod, result, lambda);

    // Queryable methods expect Expression<Func<T, TKey>>, so we use Quote()
    UnaryExpression quotedLambda;

    public Expression AppendQueryableOrder(Expression result) =>
        Expression.Call(queryableMethod, result, quotedLambda);
}
