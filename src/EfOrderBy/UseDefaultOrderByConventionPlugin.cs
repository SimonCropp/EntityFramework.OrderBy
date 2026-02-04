/// <summary>
/// Convention plugin that marks the model as having UseDefaultOrderBy() configured.
/// </summary>
sealed class UseDefaultOrderByConventionPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.ModelInitializedConventions.Add(new UseDefaultOrderByConvention());
        conventionSet.ModelFinalizingConventions.Add(new OrderByIndexConvention());
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

/// <summary>
/// Convention that creates database indexes for all configured default orderings during model finalization.
/// </summary>
sealed class OrderByIndexConvention : IModelFinalizingConvention
{
    const int maxIndexNameLength = 128;

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var annotation = entityType.FindAnnotation(OrderByExtensions.AnnotationName);
            if (annotation?.Value is not Configuration config)
            {
                continue;
            }

            var indexName = config.CustomIndexName ?? $"IX_{entityType.ClrType.Name}_DefaultOrder";

            if (indexName.Length > maxIndexNameLength)
            {
                throw new InvalidOperationException(
                    $"The auto-generated index name '{indexName}' exceeds the maximum length of {maxIndexNameLength} characters. " +
                    $"Use .WithIndexName() to specify a shorter custom index name.");
            }

            var indexBuilder = entityType.Builder.HasIndex(config.PropertyNames.ToList(), fromDataAnnotation: false);
            indexBuilder?.HasDatabaseName(indexName, fromDataAnnotation: false);
        }
    }
}
