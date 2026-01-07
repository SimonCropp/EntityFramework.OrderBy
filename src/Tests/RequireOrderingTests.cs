[TestFixture]
public class RequireOrderingTests
{
    static SqlInstance<ContextMissingOrdering> sqlInstanceWithMissing = null!;
    static SqlInstance<ContextAllOrdering> sqlInstanceWithAll = null!;
    static SqlInstance<ContextMissingOrderingNoValidation> sqlInstanceNoValidation = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        sqlInstanceWithMissing = new(
            constructInstance: builder =>
            {
                builder.UseDefaultOrderBy(requireOrderingForAllEntities: true);
                return new(builder.Options);
            },
            buildTemplate: _ => _.Database.EnsureCreatedAsync());

        sqlInstanceWithAll = new(
            constructInstance: builder =>
            {
                builder.UseDefaultOrderBy(requireOrderingForAllEntities: true);
                return new(builder.Options);
            },
            buildTemplate: _ => _.Database.EnsureCreatedAsync());

        sqlInstanceNoValidation = new(
            constructInstance: builder =>
            {
                builder.UseDefaultOrderBy(); // requireOrderingForAllEntities defaults to false
                return new(builder.Options);
            },
            buildTemplate: _ => _.Database.EnsureCreatedAsync());
    }

    [Test]
    public async Task RequireOrderingForAllEntities_ThrowsWhenEntityMissingOrdering()
    {
        await using var database = await sqlInstanceWithMissing.Build();
        await using var context = database.NewDbContext();

        context.EntitiesWithoutDefaultOrder
            .Add(
                new()
                {
                    Value = "Test"
                });
        await context.SaveChangesAsync();

        // First query should throw because EntityWithoutDefaultOrder doesn't have ordering
        var ex = Assert.ThrowsAsync<Exception>(() => context.EntitiesWithoutDefaultOrder.ToListAsync());

        Assert.That(ex!.Message, Does.Contain("EntityWithoutDefaultOrder"));
        Assert.That(ex.Message, Does.Contain("do not have ordering configured"));
    }

    [Test]
    public async Task RequireOrderingForAllEntities_SucceedsWhenAllEntitiesHaveOrdering()
    {
        await using var database = await sqlInstanceWithAll.Build();
        await using var context = database.NewDbContext();

        context.TestEntities
            .Add(
                new()
                {
                    Name = "Test",
                    CreatedDate = DateTime.Now
                });
        await context.SaveChangesAsync();

        // Should not throw
        var results = await context.TestEntities.ToListAsync();
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WithoutRequireOrdering_DoesNotThrow()
    {
        await using var database = await sqlInstanceNoValidation.Build();
        await using var context = database.NewDbContext();

        context.EntitiesWithoutDefaultOrder
            .Add(
                new()
                {
                    Value = "Test"
                });
        await context.SaveChangesAsync();

        // Should not throw
        var results = await context.EntitiesWithoutDefaultOrder.ToListAsync();
        Assert.That(results, Has.Count.EqualTo(1));
    }
}

class ContextMissingOrdering(DbContextOptions<ContextMissingOrdering> options)
    : DbContext(options)
{
    public DbSet<EntityWithoutDefaultOrder> EntitiesWithoutDefaultOrder =>
        Set<EntityWithoutDefaultOrder>();

    // Intentionally not configuring default ordering for EntityWithoutDefaultOrder
}

class ContextAllOrdering(DbContextOptions<ContextAllOrdering> options)
    : DbContext(options)
{
    public DbSet<TestEntity> TestEntities =>
        Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All entities have ordering configured
        modelBuilder.Entity<TestEntity>()
            .OrderBy(_ => _.CreatedDate);
    }
}

class ContextMissingOrderingNoValidation(DbContextOptions<ContextMissingOrderingNoValidation> options)
    : DbContext(options)
{
    public DbSet<EntityWithoutDefaultOrder> EntitiesWithoutDefaultOrder =>
        Set<EntityWithoutDefaultOrder>();

    // Intentionally not configuring default ordering for EntityWithoutDefaultOrder
}
