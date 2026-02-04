/// <summary>
/// Convention that creates database indexes for all configured default orderings during model finalization.
/// </summary>
class FinalizingConvention : IModelFinalizingConvention
{
    const int maxIndexNameLength = 128;

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            var annotation = entity.FindAnnotation(OrderByExtensions.AnnotationName);
            if (annotation?.Value is not Configuration config)
            {
                continue;
            }

            var index = config.CustomIndexName ?? $"IX_{entity.ClrType.Name}_DefaultOrder";

            if (index.Length > maxIndexNameLength)
            {
                throw new InvalidOperationException(
                    $"""
                     The auto-generated index name '{index}' exceeds the maximum length of {maxIndexNameLength} characters.
                     Use .WithIndexName() to specify a shorter custom index name.
                     """);
            }

            var builder = entity.Builder.HasIndex(config.PropertyNames, fromDataAnnotation: false);
            builder?.HasDatabaseName(index, fromDataAnnotation: false);
        }
    }
}
