/// <summary>
/// Convention that sets annotations on the model indicating UseDefaultOrderBy() was called
/// and whether index creation is enabled.
/// </summary>
class InitializedConvention(bool createIndexes) : IModelInitializedConvention
{
    public void ProcessModelInitialized(IConventionModelBuilder builder, IConventionContext<IConventionModelBuilder> context)
    {
        builder.MarkInterceptorRegistered();

        if (!createIndexes)
        {
            builder.MarkIndexCreationDisabled();
        }
    }
}
