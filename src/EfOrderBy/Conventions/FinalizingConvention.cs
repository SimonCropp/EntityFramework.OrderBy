/// <summary>
/// Convention that propagates inherited ordering, creates database indexes,
/// caches configurations, and removes annotations so they don't interfere with migration scaffolding.
/// </summary>
class FinalizingConvention : IModelFinalizingConvention
{
    const int maxIndexNameLength = 128;

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var model = modelBuilder.Metadata;
        var createIndexes = model.FindAnnotation(OrderByExtensions.IndexCreationDisabledAnnotation) == null;

        // First pass: propagate ordering from base types to derived types
        foreach (var entity in model.GetEntityTypes())
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

        // Second pass: create indexes (if enabled), cache configs, and remove annotations
        foreach (var entity in model.GetEntityTypes())
        {
            var annotation = entity.FindAnnotation(OrderByExtensions.AnnotationName);
            if (annotation?.Value is not Configuration config)
            {
                continue;
            }

            if (createIndexes && !config.IsInherited)
            {
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

            // Cache configuration for runtime use and remove annotation to prevent migration scaffold crash.
            // EF Core transforms the model between finalization and runtime (convention model → RuntimeModel),
            // so we can't attach data to the model object. Static cache keyed by entity CLR type is used instead.
            Configuration.Cache(entity.ClrType, config);
            entity.RemoveAnnotation(OrderByExtensions.AnnotationName);
        }

        // Remove model-level annotations (only needed during OnModelCreating)
        model.RemoveAnnotation(OrderByExtensions.InterceptorRegisteredAnnotation);
        model.RemoveAnnotation(OrderByExtensions.IndexCreationDisabledAnnotation);
    }
}
