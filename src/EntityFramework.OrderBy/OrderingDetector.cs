sealed class OrderingDetector : ExpressionVisitor
{
    public bool HasOrdering { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        if ((method.DeclaringType == typeof(Queryable) || method.DeclaringType == typeof(Enumerable)) &&
            method.Name is
                "OrderBy" or
                "OrderByDescending" or
                "ThenBy" or
                "ThenByDescending")
        {
            HasOrdering = true;
        }
        return base.VisitMethodCall(node);
    }
}
