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
        services.AddSingleton<IConventionSetPlugin>(_ => new ConventionPlugin(createIndexes));
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
