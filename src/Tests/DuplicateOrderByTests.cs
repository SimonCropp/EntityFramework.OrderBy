[TestFixture]
public class DuplicateOrderByTests
{
    [Test]
    public void OrderBy_CalledTwice_Throws()
    {
        var options = new DbContextOptionsBuilder<OrderByTwiceContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var context = new OrderByTwiceContext(options);
            _ = context.Model;
        });

        Assert.That(exception!.Message, Does.Contain("DuplicateTestEntity"));
        Assert.That(exception.Message, Does.Contain("ThenBy"));
    }

    [Test]
    public void OrderByDescending_CalledTwice_Throws()
    {
        var options = new DbContextOptionsBuilder<OrderByDescendingTwiceContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var context = new OrderByDescendingTwiceContext(options);
            _ = context.Model;
        });

        Assert.That(exception!.Message, Does.Contain("DuplicateTestEntity"));
        Assert.That(exception.Message, Does.Contain("ThenBy"));
    }

    [Test]
    public void OrderBy_ThenOrderByDescending_Throws()
    {
        var options = new DbContextOptionsBuilder<OrderByThenDescendingContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var context = new OrderByThenDescendingContext(options);
            _ = context.Model;
        });

        Assert.That(exception!.Message, Does.Contain("DuplicateTestEntity"));
    }

    [Test]
    public void OrderByDescending_ThenOrderBy_Throws()
    {
        var options = new DbContextOptionsBuilder<OrderByDescendingThenAscContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var context = new OrderByDescendingThenAscContext(options);
            _ = context.Model;
        });

        Assert.That(exception!.Message, Does.Contain("DuplicateTestEntity"));
    }

    [Test]
    public void OrderBy_WithThenBy_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<OrderByWithThenByContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        Assert.DoesNotThrow(() =>
        {
            using var context = new OrderByWithThenByContext(options);
            _ = context.Model;
        });
    }

    [Test]
    public void OrderByDescending_WithThenByDescending_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<OrderByDescWithThenByDescContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        Assert.DoesNotThrow(() =>
        {
            using var context = new OrderByDescWithThenByDescContext(options);
            _ = context.Model;
        });
    }

    [Test]
    public void MultipleEntities_EachCanHaveOwnOrderBy()
    {
        var options = new DbContextOptionsBuilder<MultipleEntitiesOrderByContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

        Assert.DoesNotThrow(() =>
        {
            using var context = new MultipleEntitiesOrderByContext(options);
            _ = context.Model;
        });
    }
}

#region Test Entities

public class DuplicateTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Priority { get; set; }
}

public class AnotherDuplicateTestEntity
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

#endregion

#region Test Contexts - Each with unique configuration

public class OrderByTwiceContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<DuplicateTestEntity>();
        builder.OrderBy(_ => _.Name);
        builder.OrderBy(_ => _.Priority); // Should throw
    }
}

public class OrderByDescendingTwiceContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<DuplicateTestEntity>();
        builder.OrderByDescending(_ => _.Name);
        builder.OrderByDescending(_ => _.Priority); // Should throw
    }
}

public class OrderByThenDescendingContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<DuplicateTestEntity>();
        builder.OrderBy(_ => _.Name);
        builder.OrderByDescending(_ => _.Priority); // Should throw
    }
}

public class OrderByDescendingThenAscContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<DuplicateTestEntity>();
        builder.OrderByDescending(_ => _.Name);
        builder.OrderBy(_ => _.Priority); // Should throw
    }
}

public class OrderByWithThenByContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<DuplicateTestEntity>()
            .OrderBy(_ => _.Name)
            .ThenBy(_ => _.Priority); // Correct usage
}

public class OrderByDescWithThenByDescContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<DuplicateTestEntity>()
            .OrderByDescending(_ => _.Name)
            .ThenByDescending(_ => _.Priority); // Correct usage
}

public class MultipleEntitiesOrderByContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DuplicateTestEntity> Entities => Set<DuplicateTestEntity>();
    public DbSet<AnotherDuplicateTestEntity> OtherEntities => Set<AnotherDuplicateTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DuplicateTestEntity>()
            .OrderBy(_ => _.Name);
        modelBuilder.Entity<AnotherDuplicateTestEntity>()
            .OrderByDescending(_ => _.Value); // Different entity, should work
    }
}

#endregion
