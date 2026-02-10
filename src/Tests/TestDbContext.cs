public class TestDbContext(DbContextOptions<TestDbContext> options) :
    DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<AnotherEntity> AnotherEntities => Set<AnotherEntity>();
    public DbSet<EntityWithoutDefaultOrder> EntitiesWithoutDefaultOrder => Set<EntityWithoutDefaultOrder>();
    public DbSet<EntityWithMultipleOrderings> EntitiesWithMultipleOrderings => Set<EntityWithMultipleOrderings>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeTask> EmployeeTasks => Set<EmployeeTask>();
    public DbSet<BaseEntity> BaseEntities => Set<BaseEntity>();
    public DbSet<DerivedEntityA> DerivedEntitiesA => Set<DerivedEntityA>();
    public DbSet<DerivedEntityB> DerivedEntitiesB => Set<DerivedEntityB>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        // Configure default ordering using fluent API
        model.Entity<TestEntity>()
            .OrderByDescending(_ => _.CreatedDate);

        model.Entity<AnotherEntity>()
            .Property(_ => _.Name).HasMaxLength(450);
        model.Entity<AnotherEntity>()
            .OrderBy(_ => _.Name);

        // Multiple orderings: Category ASC, then Priority DESC, then Name ASC
        model.Entity<EntityWithMultipleOrderings>()
            .Property(_ => _.Category).HasMaxLength(450);
        model.Entity<EntityWithMultipleOrderings>()
            .Property(_ => _.Name).HasMaxLength(450);
        model.Entity<EntityWithMultipleOrderings>()
            .OrderBy(_ => _.Category)
            .ThenByDescending(_ => _.Priority)
            .ThenBy(_ => _.Name);

        // EntityWithoutDefaultOrder has no default ordering configured

        // Configure Department-Employee relationship
        model.Entity<Department>()
            .HasMany(_ => _.Employees)
            .WithOne(_ => _.Department)
            .HasForeignKey(_ => _.DepartmentId)
            .IsRequired();

        // Default ordering for Department: DisplayOrder ascending
        model.Entity<Department>()
            .OrderBy(_ => _.DisplayOrder);

        // Default ordering for Employee: HireDate descending (newest first)
        model.Entity<Employee>()
            .OrderByDescending(_ => _.HireDate);

        // Configure Employee-EmployeeTask relationship
        model.Entity<EmployeeTask>()
            .HasOne(_ => _.Employee)
            .WithMany(_ => _.Tasks)
            .HasForeignKey(_ => _.EmployeeId)
            .IsRequired();

        // Default ordering for EmployeeTask: Priority ascending
        model.Entity<EmployeeTask>()
            .OrderBy(_ => _.Priority);

        // Configure TPH inheritance for BaseEntity hierarchy
        model.Entity<BaseEntity>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<BaseEntity>("Base")
            .HasValue<DerivedEntityA>("DerivedA")
            .HasValue<DerivedEntityB>("DerivedB");

        // Configure default ordering on base entity only - should inherit to derived types
        model.Entity<BaseEntity>()
            .OrderBy(_ => _.SortOrder);

        // DerivedEntityB has its own explicit ordering that should take precedence over base
        model.Entity<DerivedEntityB>()
            .Property(_ => _.Name).HasMaxLength(450);
        model.Entity<DerivedEntityB>()
            .OrderByDescending(_ => _.Name);
    }
}
