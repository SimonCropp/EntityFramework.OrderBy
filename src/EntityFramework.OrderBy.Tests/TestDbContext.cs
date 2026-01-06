public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<AnotherEntity> AnotherEntities => Set<AnotherEntity>();
    public DbSet<EntityWithoutDefaultOrder> EntitiesWithoutDefaultOrder => Set<EntityWithoutDefaultOrder>();
    public DbSet<EntityWithMultipleOrderings> EntitiesWithMultipleOrderings => Set<EntityWithMultipleOrderings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure default ordering using fluent API
        modelBuilder.Entity<TestEntity>()
            .HasDefaultOrderByDescending(e => e.CreatedDate);

        modelBuilder.Entity<AnotherEntity>()
            .HasDefaultOrderBy(e => e.Name);

        // Multiple orderings: Category ASC, then Priority DESC, then Name ASC
        modelBuilder.Entity<EntityWithMultipleOrderings>()
            .HasDefaultOrderBy(e => e.Category)
            .ThenByDescending(e => e.Priority)
            .ThenBy(e => e.Name);

        // EntityWithoutDefaultOrder has no default ordering configured
    }
}
