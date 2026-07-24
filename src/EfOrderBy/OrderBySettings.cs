namespace EfOrderBy;

/// <summary>
/// Process wide defaults for <see cref="OrderByExtensions.UseDefaultOrderBy" />.
/// </summary>
/// <remarks>
/// Intended to be set from a module initializer in a test project, so every <see cref="DbContext" />
/// in that project opts in without each <see cref="OrderByExtensions.UseDefaultOrderBy" /> call
/// having to be changed. A value passed to that method always wins, whether true or false.
/// </remarks>
public static class OrderBySettings
{
    /// <summary>
    /// The default used when the throwOnRedundantOrderBy parameter of
    /// <see cref="OrderByExtensions.UseDefaultOrderBy" /> is left null. Defaults to false.
    /// </summary>
    public static bool ThrowOnRedundantOrderBy { get; set; }
}
