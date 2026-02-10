/// <summary>
/// Convention plugin that marks the model as having UseDefaultOrderBy() configured.
/// </summary>
class ConventionPlugin(bool createIndexes, int? maxIndexableStringLength) : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventions)
    {
        conventions.ModelInitializedConventions.Add(new InitializedConvention(createIndexes));
        conventions.ModelFinalizingConventions.Add(new FinalizingConvention(maxIndexableStringLength));

        return conventions;
    }
}
