sealed class OrderRequiredExtension(bool requireOrderingForAllEntities, bool createIndexes, bool? throwOnRedundantOrderBy) :
    IDbContextOptionsExtension
{
    public bool RequireOrderingForAllEntities { get; } = requireOrderingForAllEntities;

    // Null means the caller did not set it, so OrderBySettings decides. An explicit false is
    // not the same thing: it opts this context out of a process wide setting of true.
    public bool? ThrowOnRedundantOrderBy { get; } = throwOnRedundantOrderBy;

    bool CreateIndexes { get; } = createIndexes;

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services) =>
        services.AddSingleton<IConventionSetPlugin>(services =>
        {
            // Ask the provider's type mapping source what size it uses for indexed strings.
            // For example, SQL Server's SqlServerTypeMappingSource returns 450 (nvarchar)
            // because of the 900-byte index key limit. Providers without a limit (e.g.
            // PostgreSQL, SQLite) return a mapping with Size = null.
            // https://github.com/dotnet/efcore/issues/31167
            var mappingSource = (RelationalTypeMappingSource)services.GetRequiredService<IRelationalTypeMappingSource>();
            var stringMapping = mappingSource.FindMapping(typeof(string), storeTypeName: null, keyOrIndex: true)!;
            return new ConventionPlugin(CreateIndexes, stringMapping.Size);
        });

    public void Validate(IDbContextOptions options)
    {
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) :
        DbContextOptionsExtensionInfo(extension)
    {
        new OrderRequiredExtension Extension => (OrderRequiredExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment =>
            $"{(Extension.RequireOrderingForAllEntities ? "RequireOrderingForAllEntities " : "")}" +
            $"{(Extension.CreateIndexes ? "" : "CreateIndexes=false ")}" +
            // An explicit false is worth logging, since it overrides OrderBySettings
            Extension.ThrowOnRedundantOrderBy switch
            {
                true => "ThrowOnRedundantOrderBy ",
                false => "ThrowOnRedundantOrderBy=false ",
                null => ""
            };

        public override int GetServiceProviderHashCode() =>
            HashCode.Combine(Extension.RequireOrderingForAllEntities, Extension.CreateIndexes, Extension.ThrowOnRedundantOrderBy);

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo otherInfo &&
            Extension.RequireOrderingForAllEntities == otherInfo.Extension.RequireOrderingForAllEntities &&
            Extension.CreateIndexes == otherInfo.Extension.CreateIndexes &&
            Extension.ThrowOnRedundantOrderBy == otherInfo.Extension.ThrowOnRedundantOrderBy;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["DefaultOrderBy:RequireOrderingForAllEntities"] = Extension.RequireOrderingForAllEntities.ToString();
            debugInfo["DefaultOrderBy:CreateIndexes"] = Extension.CreateIndexes.ToString();
            debugInfo["DefaultOrderBy:ThrowOnRedundantOrderBy"] = Extension.ThrowOnRedundantOrderBy?.ToString() ?? "null";
        }
    }
}
