/// <summary>
/// Convention plugin that marks the model as having UseDefaultOrderBy() configured.
/// </summary>
sealed class UseDefaultOrderByConventionPlugin(bool createIndexes) : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.ModelInitializedConventions.Add(new UseDefaultOrderByConvention(createIndexes));

        if (createIndexes)
        {
            conventionSet.ModelFinalizingConventions.Add(new OrderByIndexConvention());
        }

        return conventionSet;
    }
}

/// <summary>
/// Convention that sets annotations on the model indicating UseDefaultOrderBy() was called
/// and whether index creation is enabled.
/// </summary>
sealed class UseDefaultOrderByConvention(bool createIndexes) : IModelInitializedConvention
{
    public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        modelBuilder.HasAnnotation(OrderByExtensions.InterceptorRegisteredAnnotation, true);

        if (!createIndexes)
        {
            modelBuilder.HasAnnotation(OrderByExtensions.IndexCreationDisabledAnnotation, true);
        }
    }
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
