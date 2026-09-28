public class ThrowOnRedundantOrderBySettingTests
{
    static DbContextOptions unset =
        new DbContextOptionsBuilder<UnsetContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

    static DbContextOptions optedOut =
        new DbContextOptionsBuilder<OptedOutContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy(throwOnRedundantOrderBy: false)
            .Options;

    static DbContextOptions optedIn =
        new DbContextOptionsBuilder<OptedInContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy(throwOnRedundantOrderBy: true)
            .Options;

    [Test]
    public async Task ModuleInitializerAppliesTheSetting() =>
        await Assert.That(OrderBySettings.ThrowOnRedundantOrderBy).IsTrue();

    [Test]
    public async Task NotSetOnTheContext_UsesTheSetting()
    {
        using var context = new UnsetContext(unset);

        var exception = Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());

        await Assert.That(exception!.Message).Contains("SettingsEntity");
        await Assert.That(exception.Message).Contains("OrderBy(Name).ThenByDescending(Priority)");
    }

    [Test]
    public async Task FalseOnTheContext_OverridesTheSetting()
    {
        using var context = new OptedOutContext(optedOut);

        await Assert.That(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public void TrueOnTheContext_AgreesWithTheSetting()
    {
        using var context = new OptedInContext(optedIn);

        Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());
    }

    [Test]
    public void NotSetOnTheContext_UsesTheSettingForIncludes()
    {
        using var context = new UnsetContext(unset);

        Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .Include(_ => _.Children.OrderBy(child => child.SortOrder))
                .ToQueryString());
    }

    [Test]
    public async Task FalseOnTheContext_OverridesTheSettingForIncludes()
    {
        using var context = new OptedOutContext(optedOut);

        await Assert.That(
            () => context.Entities
                .Include(_ => _.Children.OrderBy(child => child.SortOrder))
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task NonRedundantOrdering_DoesNotThrow()
    {
        using var context = new UnsetContext(unset);

        await Assert.That(
            () => context.Entities
                .OrderByDescending(_ => _.Name)
                .ToQueryString()).ThrowsNothing();
    }
}

public class SettingsEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Priority { get; set; }
    public List<SettingsChild> Children { get; set; } = [];
}

public class SettingsChild
{
    public int Id { get; set; }
    public int SettingsEntityId { get; set; }
    public SettingsEntity Parent { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class UnsetContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<SettingsEntity> Entities => Set<SettingsEntity>();

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder.ConfigureSettingsEntities();
}

public class OptedOutContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<SettingsEntity> Entities => Set<SettingsEntity>();

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder.ConfigureSettingsEntities();
}

public class OptedInContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<SettingsEntity> Entities => Set<SettingsEntity>();

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder.ConfigureSettingsEntities();
}

static class SettingsModelBuilder
{
    // All three contexts share these entities, so the configuration must match
    public static void ConfigureSettingsEntities(this ModelBuilder builder)
    {
        builder.Entity<SettingsEntity>()
            .HasMany(_ => _.Children)
            .WithOne(_ => _.Parent)
            .HasForeignKey(_ => _.SettingsEntityId)
            .IsRequired();

        builder.Entity<SettingsEntity>()
            .Property(_ => _.Name).HasMaxLength(450);

        builder.Entity<SettingsEntity>()
            .OrderBy(_ => _.Name)
            .ThenByDescending(_ => _.Priority);

        builder.Entity<SettingsChild>()
            .OrderBy(_ => _.SortOrder);
    }
}
