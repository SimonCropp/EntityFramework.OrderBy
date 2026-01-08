/// <summary>
/// Convention plugin that marks the model as having UseDefaultOrderBy() configured.
/// </summary>
sealed class UseDefaultOrderByConventionPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.ModelInitializedConventions.Add(new UseDefaultOrderByConvention());
        return conventionSet;
    }
}

/// <summary>
/// Convention that sets an annotation on the model indicating UseDefaultOrderBy() was called.
/// </summary>
sealed class UseDefaultOrderByConvention : IModelInitializedConvention
{
    public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context) =>
        modelBuilder.HasAnnotation(OrderByExtensions.InterceptorRegisteredAnnotation, true);
}
