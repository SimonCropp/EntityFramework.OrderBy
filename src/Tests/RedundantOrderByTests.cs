public class RedundantOrderByTests
{
    static DbContextOptions enabled =
        new DbContextOptionsBuilder<RedundantEnabledContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy(throwOnRedundantOrderBy: true)
            .Options;

    static DbContextOptions disabled =
        new DbContextOptionsBuilder<RedundantDisabledContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

    [Test]
    public async Task ExactMatch_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        var exception = Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());

        await Assert.That(exception!.Message).Contains("RedundantEntity");
        await Assert.That(exception.Message).Contains("OrderBy(Name).ThenByDescending(Priority)");
    }

    [Test]
    public void ExactMatchAfterWhere_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .Where(_ => _.Priority > 1)
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());
    }

    [Test]
    public void ExactMatchBeforeWhere_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .Where(_ => _.Priority > 1)
                .ToQueryString());
    }

    [Test]
    public void ExactMatchWithSelect_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .Select(_ => _.Name)
                .ToQueryString());
    }

    [Test]
    public void ExactMatchOnSingleClause_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.ThrowsExactly<Exception>(
            () => context.Children
                .OrderBy(_ => _.SortOrder)
                .ToQueryString());
    }

    [Test]
    public async Task PartialMatch_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task ExtraClause_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ThenBy(_ => _.Id)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task DifferentDirection_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .OrderByDescending(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task DifferentProperty_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .OrderBy(_ => _.Id)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task ReorderedClauses_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .OrderByDescending(_ => _.Priority)
                .ThenBy(_ => _.Name)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task NoExplicitOrdering_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task EntityWithoutConfiguration_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Unordered
                .OrderBy(_ => _.Value)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task Include_ExactMatch_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        var exception = Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .Include(_ => _.Children.OrderBy(child => child.SortOrder))
                .ToQueryString());

        await Assert.That(exception!.Message).Contains("RedundantChild");
        await Assert.That(exception.Message).Contains("OrderBy(SortOrder)");
    }

    [Test]
    public void Include_ExactMatchWithFilter_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.ThrowsExactly<Exception>(
            () => context.Entities
                .Include(_ => _.Children
                    .Where(child => child.SortOrder > 0)
                    .OrderBy(child => child.SortOrder))
                .ToQueryString());
    }

    [Test]
    public async Task Include_DifferentOrdering_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .Include(_ => _.Children.OrderBy(child => child.Title))
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task Include_WithoutOrdering_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .Include(_ => _.Children)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task ExactMatchInsideConcat_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        // The default ordering is not applied to a combined sequence, so ordering one
        // side of it is not redundant
        await Assert.That(
            () => context.Entities
                .Where(_ => _.Priority > 1)
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .Concat(context.Entities.Where(_ => _.Priority <= 1))
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task ExactMatchInsideJoin_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        await Assert.That(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .Join(
                    context.Children,
                    entity => entity.Id,
                    child => child.RedundantEntityId,
                    (entity, child) => child.Title)
                .ToQueryString()).ThrowsNothing();
    }

    [Test]
    public async Task Disabled_DoesNotThrow()
    {
        using var context = new RedundantDisabledContext(disabled);

        await Assert.That(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString()).ThrowsNothing();
    }
}

public class RedundantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Priority { get; set; }
    public List<RedundantChild> Children { get; set; } = [];
}

public class RedundantChild
{
    public int Id { get; set; }
    public int RedundantEntityId { get; set; }
    public RedundantEntity Parent { get; set; } = null!;
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
}

public class RedundantUnorderedEntity
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

public class RedundantEnabledContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<RedundantEntity> Entities => Set<RedundantEntity>();
    public DbSet<RedundantChild> Children => Set<RedundantChild>();
    public DbSet<RedundantUnorderedEntity> Unordered => Set<RedundantUnorderedEntity>();

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder.ConfigureRedundantEntities();
}

public class RedundantDisabledContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<RedundantEntity> Entities => Set<RedundantEntity>();
    public DbSet<RedundantChild> Children => Set<RedundantChild>();

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder.ConfigureRedundantEntities();
}

static class RedundantModelBuilder
{
    // Both contexts share these entities, so the configuration must match
    public static void ConfigureRedundantEntities(this ModelBuilder builder)
    {
        builder.Entity<RedundantEntity>()
            .HasMany(_ => _.Children)
            .WithOne(_ => _.Parent)
            .HasForeignKey(_ => _.RedundantEntityId)
            .IsRequired();

        builder.Entity<RedundantEntity>()
            .OrderBy(_ => _.Name)
            .ThenByDescending(_ => _.Priority);

        builder.Entity<RedundantChild>()
            .OrderBy(_ => _.SortOrder);
    }
}
