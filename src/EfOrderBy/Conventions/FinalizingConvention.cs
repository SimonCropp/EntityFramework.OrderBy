/// <summary>
/// Convention that propagates inherited ordering and creates database indexes
/// for all configured default orderings during model finalization.
/// </summary>
class FinalizingConvention : IModelFinalizingConvention
{
    const int maxIndexNameLength = 128;

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        // First pass: propagate ordering from base types to derived types
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            // Skip if this entity already has its own ordering configured
            if (entity.FindAnnotation(OrderByExtensions.AnnotationName)?.Value is Configuration)
            {
                continue;
            }

            // Walk up the base type chain to find inherited ordering
            var baseType = entity.BaseType;
            while (baseType != null)
            {
                if (baseType.FindAnnotation(OrderByExtensions.AnnotationName)?.Value is Configuration baseConfig)
                {
                    var derivedConfig = baseConfig.CreateForDerivedType(entity.ClrType);
                    entity.SetAnnotation(OrderByExtensions.AnnotationName, derivedConfig);
                    break;
                }

                baseType = baseType.BaseType;
            }
        }

        // Second pass: create indexes for non-inherited configurations
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes())
        {
            var annotation = entity.FindAnnotation(OrderByExtensions.AnnotationName);
            if (annotation?.Value is not Configuration config)
            {
                continue;
            }

            // Skip index creation for inherited configurations
            // (the base type's index already covers the same columns in TPH)
            if (config.IsInherited)
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
