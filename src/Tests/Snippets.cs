// ReSharper disable All
#pragma warning disable IDE0022
namespace Snippets;

public class EnableInterceptorExample : DbContext
{
    #region EnableInterceptor

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.UseDefaultOrderBy();

    #endregion
}

public class ConfigureOrderingExample : DbContext
{
    #region ConfigureOrdering

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Employee>()
            .OrderBy(_ => _.HireDate)
            .ThenByDescending(_ => _.Salary);

        builder.Entity<Department>()
            .OrderBy(_ => _.DisplayOrder);
    }

    #endregion
}

public class RequireOrderingExample : DbContext
{
    #region RequireOrdering

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.UseDefaultOrderBy(
            requireOrderingForAllEntities: true);

    #endregion
}

public class ThrowOnRedundantOrderByExample : DbContext
{
    #region ThrowOnRedundantOrderBy

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.UseDefaultOrderBy(
            throwOnRedundantOrderBy: true);

    #endregion
}

public class DisableIndexCreationExample : DbContext
{
    #region DisableIndexCreation

    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.UseDefaultOrderBy(
            createIndexes: false);

    #endregion
}

public class SnippetExamples
{
    static async Task QueryWithoutOrderBy()
    {
        AppDbContext context = null!;

        #region QueryWithoutOrderBy

        // Automatically ordered by HireDate, then Salary descending
        var employees = await context.Employees
            .ToListAsync();

        // Explicit ordering takes precedence
        var employeesByName = await context.Employees
            .OrderBy(_ => _.Name)
            .ToListAsync();

        #endregion
    }

    static async Task PagingAndSingleResults()
    {
        AppDbContext context = null!;

        #region PagingAndSingleResults

        // The ordering is applied before the page is taken, so the page is
        // taken from an ordered sequence rather than sorted after the fact
        var secondPage = await context.Employees
            .Skip(20)
            .Take(20)
            .ToListAsync();

        // Ordered by HireDate, then Salary descending, so this is the
        // earliest hire rather than an arbitrary row
        var first = await context.Employees
            .FirstAsync();

        #endregion
    }

    static async Task IncludeSupport()
    {
        AppDbContext context = null!;

        #region IncludeSupport

        // Departments ordered by DisplayOrder
        // Employees ordered by HireDate, then Salary descending
        var departments = await context.Departments
            .Include(_ => _.Employees)
            .ToListAsync();

        #endregion
    }

    static void MultiColumnOrdering(ModelBuilder builder)
    {
        #region MultiColumnOrdering

        builder.Entity<Product>()
            .OrderBy(_ => _.Category)
            .ThenBy(_ => _.Name)
            .ThenByDescending(_ => _.Price);

        #endregion
    }
}

#region CompleteExample

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }
    public List<Employee> Employees { get; set; } = [];
}

public class Employee
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = "";
    public DateTime HireDate { get; set; }
    public int Salary { get; set; }
}

public class AppDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder
            .UseSqlServer("connection-string")
            .UseDefaultOrderBy();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Department>()
            .OrderBy(_ => _.DisplayOrder);

        builder.Entity<Employee>()
            .OrderBy(_ => _.HireDate)
            .ThenByDescending(_ => _.Salary);
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
}

#endregion

class Product
{
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

#region InheritanceOrdering

public class BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class DerivedEntityA : BaseEntity
{
    public string ExtraA { get; set; } = "";
}

public class DerivedEntityB : BaseEntity
{
    public string ExtraB { get; set; } = "";
}

public class InheritanceDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder
            .UseSqlServer("connection-string")
            .UseDefaultOrderBy();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configure ordering on the base entity
        // DerivedEntityA and DerivedEntityB automatically inherit this ordering
        builder.Entity<BaseEntity>()
            .OrderBy(_ => _.SortOrder);

        // Optionally, a derived type can override with its own ordering
        builder.Entity<DerivedEntityB>()
            .OrderByDescending(_ => _.Name);
    }

    public DbSet<BaseEntity> BaseEntities => Set<BaseEntity>();
    public DbSet<DerivedEntityA> DerivedEntitiesA => Set<DerivedEntityA>();
    public DbSet<DerivedEntityB> DerivedEntitiesB => Set<DerivedEntityB>();
}

#endregion
