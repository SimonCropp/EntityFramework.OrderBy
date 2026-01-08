[TestFixture]
public class InterceptorRegistrationTests
{
    [Test]
    public void OrderBy_ThrowsWhenUseDefaultOrderByNotCalled()
    {
        var options = new DbContextOptionsBuilder<ContextWithoutUseDefaultOrderBy>()
            .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True")
            .Options;

        var exception = Assert.Throws<Exception>(() =>
        {
            using var context = new ContextWithoutUseDefaultOrderBy(options);
            // Force model creation
            _ = context.Model;
        })!;

        Assert.That(exception.Message, Does.Contain("UseDefaultOrderBy()"));
        Assert.That(exception.Message, Does.Contain("must be called"));
    }

    [Test]
    public void OrderByDescending_ThrowsWhenUseDefaultOrderByNotCalled()
    {
        var options = new DbContextOptionsBuilder<ContextWithoutUseDefaultOrderByDescending>()
            .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True")
            .Options;

        var exception = Assert.Throws<Exception>(() =>
        {
            using var context = new ContextWithoutUseDefaultOrderByDescending(options);
            // Force model creation
            _ = context.Model;
        })!;

        Assert.That(exception.Message, Does.Contain("UseDefaultOrderBy()"));
        Assert.That(exception.Message, Does.Contain("must be called"));
    }

    [Test]
    public void OrderBy_SucceedsWhenUseDefaultOrderByCalled()
    {
        var options = new DbContextOptionsBuilder<ContextWithUseDefaultOrderBy>()
            .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True")
            .UseDefaultOrderBy()
            .Options;

        // Should not throw
        using var context = new ContextWithUseDefaultOrderBy(options);
        _ = context.Model;
    }
}

class ContextWithoutUseDefaultOrderBy(DbContextOptions<ContextWithoutUseDefaultOrderBy> options)
    : DbContext(options)
{
    public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This should throw because UseDefaultOrderBy was not called
        modelBuilder.Entity<SimpleEntity>()
            .OrderBy(_ => _.Name);
    }
}

class ContextWithoutUseDefaultOrderByDescending(DbContextOptions<ContextWithoutUseDefaultOrderByDescending> options)
    : DbContext(options)
{
    public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This should throw because UseDefaultOrderBy was not called
        modelBuilder.Entity<SimpleEntity>()
            .OrderByDescending(_ => _.Name);
    }
}

class ContextWithUseDefaultOrderBy(DbContextOptions options)
    : DbContext(options)
{
    public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This should succeed because UseDefaultOrderBy was called
        modelBuilder.Entity<SimpleEntity>()
            .OrderBy(_ => _.Name);
    }
}

public class SimpleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
