/// <summary>
/// Resolves the chain of properties a key selector reads.
/// </summary>
/// <remarks>
/// A chain of more than one property reaches through an owned type, which is how a property of a
/// JSON mapped column is addressed, for example <c>_ => _.Metadata.Rank</c>. EF Core translates
/// those to a read of the JSON document, so the whole chain is kept rather than only its last
/// property.
/// </remarks>
static class PropertyPath
{
    public static PropertyInfo[] Resolve<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> property)
    {
        if (TryResolve(property.Body) is { } path)
        {
            return path;
        }

        throw new ArgumentException("Expression must be a property access expression", nameof(property));
    }

    /// <summary>
    /// Returns the properties of a member chain rooted at the lambda parameter, outermost last,
    /// or null when the expression is not such a chain.
    /// </summary>
    public static PropertyInfo[]? TryResolve(Expression? expression)
    {
        var segments = new List<PropertyInfo>();

        while (expression is MemberExpression {Member: PropertyInfo property} member)
        {
            segments.Add(property);
            expression = member.Expression;
        }

        // A chain that does not bottom out at the parameter, for example one rooted at a captured
        // variable, a constant, or a method call, cannot be expressed as ordering configuration
        if (segments.Count == 0 ||
            expression is not ParameterExpression)
        {
            return null;
        }

        segments.Reverse();
        return [..segments];
    }

    public static string Describe(IEnumerable<PropertyInfo> path) =>
        string.Join('.', path.Select(_ => _.Name));
}
