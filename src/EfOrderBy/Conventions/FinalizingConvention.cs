/// <summary>
/// Convention that propagates inherited ordering, creates database indexes,
/// caches configurations, and removes annotations so they don't interfere with migration scaffolding.
/// </summary>
class FinalizingConvention(int? maxIndexableStringLength) : IModelFinalizingConvention
{
    const int maxIndexNameLength = 128;

    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        var model = modelBuilder.Metadata;
        var createIndexes = !model.IsIndexCreationDisabled();

        // First pass: propagate ordering from base types to derived types
        foreach (var entity in model.GetEntityTypes())
        {
            // Skip if this entity already has its own ordering configured
            if (entity.GetOrderByConfiguration() is not null)
            {
                continue;
            }

            // Walk up the base type chain to find inherited ordering
            var baseType = entity.BaseType;
            while (baseType != null)
            {
                if (baseType.GetOrderByConfiguration() is { } baseConfig)
                {
                    var derivedConfig = baseConfig.CreateForDerivedType(entity.ClrType);
                    entity.SetOrderByConfiguration(derivedConfig);
                    break;
                }

                baseType = baseType.BaseType;
            }
        }

        // Second pass: create indexes (if enabled), cache configs, and remove annotations
        foreach (var entity in model.GetEntityTypes())
        {
            var config = entity.GetOrderByConfiguration();
            if (config is null)
            {
                continue;
            }

            if (createIndexes && !config.IsInherited && !HasLargeStringProperty(entity, config, maxIndexableStringLength))
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
            entity.RemoveOrderByConfiguration();
        }

        // Remove model-level annotations (only needed during OnModelCreating)
        model.RemoveOrderByAnnotations();
    }

    // Some database providers silently cap string column lengths when an index is added.
    // For example, SQL Server caps nvarchar to 450 chars due to the 900-byte index key limit.
    // https://github.com/dotnet/efcore/issues/31167
    // The maxIndexableStringLength is derived from the provider's own RelationalTypeMappingSource
    // by querying FindMapping(typeof(string), keyOrIndex: true).Size.
    // When null, the provider has no limit and we never skip.
    static bool HasLargeStringProperty(IConventionEntityType entity, Configuration config, int? maxIndexableLength)
    {
        if (maxIndexableLength is null)
        {
            return false;
        }

        foreach (var propertyName in config.PropertyNames)
        {
            var property = entity.FindProperty(propertyName);
            if (property?.ClrType != typeof(string))
            {
                continue;
            }

            var maxLength = property.GetMaxLength();
            if (maxLength is null || maxLength > maxIndexableLength)
            {
                return true;
            }
        }

        return false;
    }
}
