public class TestDbContext(DbContextOptions<TestDbContext> options) :
    DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<AnotherEntity> AnotherEntities => Set<AnotherEntity>();
    public DbSet<EntityWithoutDefaultOrder> EntitiesWithoutDefaultOrder => Set<EntityWithoutDefaultOrder>();
    public DbSet<EntityWithMultipleOrderings> EntitiesWithMultipleOrderings => Set<EntityWithMultipleOrderings>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure default ordering using fluent API
        modelBuilder.Entity<TestEntity>()
            .OrderByDescending(_ => _.CreatedDate);

        modelBuilder.Entity<AnotherEntity>()
            .OrderBy(_ => _.Name);

        // Multiple orderings: Category ASC, then Priority DESC, then Name ASC
        modelBuilder.Entity<EntityWithMultipleOrderings>()
            .OrderBy(_ => _.Category)
            .ThenByDescending(_ => _.Priority)
            .ThenBy(_ => _.Name);

        // EntityWithoutDefaultOrder has no default ordering configured

        // Configure Department-Employee relationship
        modelBuilder.Entity<Department>()
            .HasMany(_ => _.Employees)
            .WithOne(_ => _.Department)
            .HasForeignKey(_ => _.DepartmentId)
            .IsRequired();

        // Default ordering for Department: DisplayOrder ascending
        modelBuilder.Entity<Department>()
            .OrderBy(_ => _.DisplayOrder);

        // Default ordering for Employee: HireDate descending (newest first)
        modelBuilder.Entity<Employee>()
            .OrderByDescending(_ => _.HireDate);
    }
}
