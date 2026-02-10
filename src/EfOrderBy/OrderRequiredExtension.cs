/// <summary>
/// Options extension to store default ordering configuration.
/// </summary>
sealed class OrderRequiredExtension(bool requireOrderingForAllEntities, bool createIndexes) :
    IDbContextOptionsExtension
{
    public bool RequireOrderingForAllEntities { get; } = requireOrderingForAllEntities;
    public bool CreateIndexes { get; } = createIndexes;

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        var createIndexes = CreateIndexes;
        services.AddSingleton<IConventionSetPlugin>(sp =>
        {
            // Ask the provider's type mapping source what size it uses for indexed strings.
            // For example, SQL Server's SqlServerTypeMappingSource returns 450 (nvarchar)
            // because of the 900-byte index key limit. Providers without a limit (e.g.
            // PostgreSQL, SQLite) return a mapping with Size = null.
            // https://github.com/dotnet/efcore/issues/31167
            var mappingSource = (RelationalTypeMappingSource)sp.GetRequiredService<IRelationalTypeMappingSource>();
            var indexedStringMapping = mappingSource.FindMapping(typeof(string), storeTypeName: null, keyOrIndex: true);
            var maxIndexableStringLength = indexedStringMapping!.Size;
            return new ConventionPlugin(createIndexes, maxIndexableStringLength);
        });
    }

    public void Validate(IDbContextOptions options)
    {
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        new OrderRequiredExtension Extension => (OrderRequiredExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment =>
            $"{(Extension.RequireOrderingForAllEntities ? "RequireOrderingForAllEntities " : "")}" +
            $"{(Extension.CreateIndexes ? "" : "CreateIndexes=false ")}";

        public override int GetServiceProviderHashCode() =>
            HashCode.Combine(Extension.RequireOrderingForAllEntities, Extension.CreateIndexes);

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo otherInfo &&
            Extension.RequireOrderingForAllEntities == otherInfo.Extension.RequireOrderingForAllEntities &&
            Extension.CreateIndexes == otherInfo.Extension.CreateIndexes;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["DefaultOrderBy:RequireOrderingForAllEntities"] =
                Extension.RequireOrderingForAllEntities.ToString();
            debugInfo["DefaultOrderBy:CreateIndexes"] =
                Extension.CreateIndexes.ToString();
        }
    }
}
