sealed class OrderingDetector : ExpressionVisitor
{
    public bool HasOrdering { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Check if this is an Include/ThenInclude - don't look for ordering inside its lambda
        if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
            (node.Method.Name == "Include" || node.Method.Name == "ThenInclude"))
        {
            // Visit the source (first argument) but skip the lambda (second argument)
            // to avoid detecting ordering within Include(_ => _.Collection.OrderBy(...))
            Visit(node.Arguments[0]);
            return node;
        }

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
