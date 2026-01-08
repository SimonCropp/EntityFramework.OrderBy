/// <summary>
/// Options extension to store default ordering configuration.
/// </summary>
sealed class OrderRequiredExtension(bool requireOrderingForAllEntities) :
    IDbContextOptionsExtension
{
    public bool RequireOrderingForAllEntities { get; } = requireOrderingForAllEntities;

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services) =>
        services.AddSingleton<IConventionSetPlugin, UseDefaultOrderByConventionPlugin>();

    public void Validate(IDbContextOptions options)
    {
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        new OrderRequiredExtension Extension => (OrderRequiredExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment =>
            Extension.RequireOrderingForAllEntities
                ? "RequireOrderingForAllEntities "
                : "";

        public override int GetServiceProviderHashCode() => Extension.RequireOrderingForAllEntities.GetHashCode();

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo otherInfo &&
            Extension.RequireOrderingForAllEntities == otherInfo.Extension.RequireOrderingForAllEntities;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) =>
            debugInfo["DefaultOrderBy:RequireOrderingForAllEntities"] =
                Extension.RequireOrderingForAllEntities.ToString();
    }
}
