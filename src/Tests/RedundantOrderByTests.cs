[TestFixture]
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
    public void ExactMatch_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        var exception = Assert.Throws<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());

        Assert.That(exception!.Message, Does.Contain("RedundantEntity"));
        Assert.That(exception.Message, Does.Contain("OrderBy(Name).ThenByDescending(Priority)"));
    }

    [Test]
    public void ExactMatchAfterWhere_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.Throws<Exception>(
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

        Assert.Throws<Exception>(
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

        Assert.Throws<Exception>(
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

        Assert.Throws<Exception>(
            () => context.Children
                .OrderBy(_ => _.SortOrder)
                .ToQueryString());
    }

    [Test]
    public void PartialMatch_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ToQueryString());
    }

    [Test]
    public void ExtraClause_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ThenBy(_ => _.Id)
                .ToQueryString());
    }

    [Test]
    public void DifferentDirection_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderByDescending(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());
    }

    [Test]
    public void DifferentProperty_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderBy(_ => _.Id)
                .ToQueryString());
    }

    [Test]
    public void ReorderedClauses_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderByDescending(_ => _.Priority)
                .ThenBy(_ => _.Name)
                .ToQueryString());
    }

    [Test]
    public void NoExplicitOrdering_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .ToQueryString());
    }

    [Test]
    public void EntityWithoutConfiguration_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Unordered
                .OrderBy(_ => _.Value)
                .ToQueryString());
    }

    [Test]
    public void Include_ExactMatch_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        var exception = Assert.Throws<Exception>(
            () => context.Entities
                .Include(_ => _.Children.OrderBy(child => child.SortOrder))
                .ToQueryString());

        Assert.That(exception!.Message, Does.Contain("RedundantChild"));
        Assert.That(exception.Message, Does.Contain("OrderBy(SortOrder)"));
    }

    [Test]
    public void Include_ExactMatchWithFilter_Throws()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.Throws<Exception>(
            () => context.Entities
                .Include(_ => _.Children
                    .Where(child => child.SortOrder > 0)
                    .OrderBy(child => child.SortOrder))
                .ToQueryString());
    }

    [Test]
    public void Include_DifferentOrdering_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .Include(_ => _.Children.OrderBy(child => child.Title))
                .ToQueryString());
    }

    [Test]
    public void Include_WithoutOrdering_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .Include(_ => _.Children)
                .ToQueryString());
    }

    [Test]
    public void ExactMatchInsideConcat_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        // The default ordering is not applied to a combined sequence, so ordering one
        // side of it is not redundant
        Assert.DoesNotThrow(
            () => context.Entities
                .Where(_ => _.Priority > 1)
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .Concat(context.Entities.Where(_ => _.Priority <= 1))
                .ToQueryString());
    }

    [Test]
    public void ExactMatchInsideJoin_DoesNotThrow()
    {
        using var context = new RedundantEnabledContext(enabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .Join(
                    context.Children,
                    entity => entity.Id,
                    child => child.RedundantEntityId,
                    (entity, child) => child.Title)
                .ToQueryString());
    }

    [Test]
    public void Disabled_DoesNotThrow()
    {
        using var context = new RedundantDisabledContext(disabled);

        Assert.DoesNotThrow(
            () => context.Entities
                .OrderBy(_ => _.Name)
                .ThenByDescending(_ => _.Priority)
                .ToQueryString());
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
