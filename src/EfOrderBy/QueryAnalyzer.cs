sealed class QueryAnalyzer :
    ExpressionVisitor
{
    public bool HasOrdering { get; private set; }
    public bool HasInclude { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var name = node.Method.Name;
        var type = node.Method.DeclaringType;

        // Check if this is an Include/ThenInclude - don't look for ordering inside its lambda
        if (type == typeof(EntityFrameworkQueryableExtensions) &&
            name is "Include" or "ThenInclude")
        {
            HasInclude = true;
            // Visit the source (first argument) but skip the lambda (second argument)
            // to avoid detecting ordering within Include(_ => _.Collection.OrderBy(...))
            Visit(node.Arguments[0]);
            return node;
        }

        // Check if this is a Select - don't look for ordering inside its lambda
        if (type == typeof(Queryable) &&
            name == "Select")
        {
            // Visit the source (first argument) but skip the lambda (second argument)
            // to avoid detecting ordering within Select(_ => new { ... _.Children.OrderBy(...) })
            Visit(node.Arguments[0]);
            return node;
        }

        if ((type == typeof(Queryable) ||
             type == typeof(Enumerable)) &&
            name is
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
