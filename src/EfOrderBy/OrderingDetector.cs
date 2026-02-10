sealed class OrderingDetector :
    ExpressionVisitor
{
    public bool HasOrdering { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        // Check if this is an Include/ThenInclude - don't look for ordering inside its lambda
        if (method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
            method.Name is "Include" or "ThenInclude")
        {
            // Visit the source (first argument) but skip the lambda (second argument)
            // to avoid detecting ordering within Include(_ => _.Collection.OrderBy(...))
            Visit(node.Arguments[0]);
            return node;
        }

        // Check if this is a Select - don't look for ordering inside its lambda
        if (method.DeclaringType == typeof(Queryable) &&
            method.Name == "Select")
        {
            // Visit the source (first argument) but skip the lambda (second argument)
            // to avoid detecting ordering within Select(_ => new { ... _.Children.OrderBy(...) })
            Visit(node.Arguments[0]);
            return node;
        }

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
