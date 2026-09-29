#pragma warning disable TUnit0023 // LocalDb instances are intentionally left open
public class RequireOrderingTests
{
    static SqlInstance<ContextAllOrdering> sqlInstanceWithAll = null!;
    static SqlInstance<ContextMissingOrderingNoValidation> sqlInstanceNoValidation = null!;

    [Before(Class)]
    public static void Setup()
    {
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

    // Ordering is validated when the query is compiled, before a connection is opened, so the
    // throwing tests use a context whose connection is never opened
    static ContextMissingOrdering NewModelOnlyContextMissingOrdering()
    {
        var builder = new DbContextOptionsBuilder<ContextMissingOrdering>()
            .UseSqlServer("Server=.;Database=Test;");
        builder.UseDefaultOrderBy(requireOrderingForAllEntities: true);
        return new(builder.Options);
    }

    [Test]
    public async Task RequireOrderingForAllEntities_ThrowsWhenEntityMissingOrdering()
    {
        await using var context = NewModelOnlyContextMissingOrdering();

        // First query should throw because EntityWithoutDefaultOrder doesn't have ordering
        var ex = await Assert.ThrowsExactlyAsync<Exception>(() => context.EntitiesWithoutDefaultOrder.ToListAsync());

        await Assert.That(ex!.Message).Contains("EntityWithoutDefaultOrder");
        await Assert.That(ex.Message).Contains("do not have ordering configured");
    }

    [Test]
    public async Task RequireOrderingForAllEntities_ThrowsOnEveryQueryNotJustTheFirst()
    {
        await using var context = NewModelOnlyContextMissingOrdering();

        await Assert.ThrowsExactlyAsync<Exception>(() => context.EntitiesWithoutDefaultOrder.ToListAsync());

        // Validation is cached per DbContext type. A failed validation must not be cached,
        // otherwise the error disappears after the first query and later queries silently
        // return unordered results
        var exception = await Assert.ThrowsExactlyAsync<Exception>(() => context.EntitiesWithoutDefaultOrder.ToListAsync());

        await Assert.That(exception!.Message).Contains("EntityWithoutDefaultOrder");
        await Assert.That(exception.Message).Contains("do not have ordering configured");
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
        await Assert.That(results).Count().IsEqualTo(1);
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
        await Assert.That(results).Count().IsEqualTo(1);
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

        // All entities have ordering configured (must match TestDbContext's ordering)
        modelBuilder.Entity<TestEntity>()
            .OrderByDescending(_ => _.CreatedDate);
    }
}

class ContextMissingOrderingNoValidation(DbContextOptions<ContextMissingOrderingNoValidation> options)
    : DbContext(options)
{
    public DbSet<EntityWithoutDefaultOrder> EntitiesWithoutDefaultOrder =>
        Set<EntityWithoutDefaultOrder>();

    // Intentionally not configuring default ordering for EntityWithoutDefaultOrder
}
