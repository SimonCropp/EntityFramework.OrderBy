/// <summary>
/// Scans the outermost chain of a query for existing ordering and for Include calls.
/// </summary>
/// <remarks>
/// Only the chain is walked, never the lambdas passed to it. Ordering nested inside a lambda,
/// for example Where(_ => _.Children.OrderBy(child => child.Name).Any()), orders a subquery and
/// must not suppress the default ordering of the query it is nested in.
/// </remarks>
static class QueryAnalyzer
{
    public static (bool HasOrdering, bool HasInclude) Analyze(Expression expression)
    {
        var hasOrdering = false;
        var hasInclude = false;

        while (expression is MethodCallExpression call)
        {
            var method = call.Method;
            var declaringType = method.DeclaringType;
            var name = method.Name;

            if (declaringType == typeof(EntityFrameworkQueryableExtensions) &&
                name is "Include" or "ThenInclude")
            {
                hasInclude = true;
            }
            else if ((declaringType == typeof(Queryable) ||
                      declaringType == typeof(Enumerable)) &&
                     name is
                         "OrderBy" or
                         "OrderByDescending" or
                         "ThenBy" or
                         "ThenByDescending")
            {
                hasOrdering = true;
            }

            // Only static operators compose a chain, and the source is always their first argument
            if (!method.IsStatic ||
                call.Arguments.Count == 0)
            {
                break;
            }

            expression = call.Arguments[0];
        }

        return (hasOrdering, hasInclude);
    }

    public static bool HasOrdering(Expression expression) =>
        Analyze(expression).HasOrdering;
}
