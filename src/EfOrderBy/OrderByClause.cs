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

    static MethodInfo FindMethod(MethodInfo[] methods, string name) =>
        methods.First(_ => _.Name == name &&
                           _.GetParameters().Length == 2);

    static MethodInfo[] queryableMethods = typeof(Queryable).GetMethods();
    static MethodInfo[] enumerableMethods = typeof(Enumerable).GetMethods();

    static MethodInfo queryableOrderBy = FindMethod(queryableMethods, nameof(Queryable.OrderBy));
    static MethodInfo queryableOrderByDescending = FindMethod(queryableMethods, nameof(Queryable.OrderByDescending));
    static MethodInfo queryableThenBy = FindMethod(queryableMethods, nameof(Queryable.ThenBy));
    static MethodInfo queryableThenByDescending = FindMethod(queryableMethods, nameof(Queryable.ThenByDescending));

    static MethodInfo enumerableOrderBy = FindMethod(enumerableMethods, nameof(Enumerable.OrderBy));
    static MethodInfo enumerableOrderByDescending = FindMethod(enumerableMethods, nameof(Enumerable.OrderByDescending));
    static MethodInfo enumerableThenBy = FindMethod(enumerableMethods, nameof(Enumerable.ThenBy));
    static MethodInfo enumerableThenByDescending = FindMethod(enumerableMethods, nameof(Enumerable.ThenByDescending));
    LambdaExpression lambda;

    // The fully generic Enumerable method (e.g., OrderBy<ParentEntity, string>)
    // ready to be invoked without further generic type arguments.
    MethodInfo enumerableMethod;

    // The fully generic Queryable method (e.g., OrderBy<ParentEntity, string>)
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
