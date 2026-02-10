/// <summary>
/// Convention plugin that marks the model as having UseDefaultOrderBy() configured.
/// </summary>
class ConventionPlugin(bool createIndexes) : IConventionSetPlugin
{
    static FinalizingConvention finalizingConvention = new();

    public ConventionSet ModifyConventions(ConventionSet conventions)
    {
        conventions.ModelInitializedConventions.Add(new InitializedConvention(createIndexes));
        conventions.ModelFinalizingConventions.Add(finalizingConvention);

        return conventions;
    }
}
