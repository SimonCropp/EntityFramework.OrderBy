// ReSharper disable All
#pragma warning disable IDE0022
using Microsoft.EntityFrameworkCore;

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
            .ThenByDescending(e => e.Salary);

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

public class SnippetExamples
{
    static async Task QueryWithoutOrderBy()
    {
        DbContext context = null!;

        #region QueryWithoutOrderBy

        // Automatically ordered by HireDate, then Salary descending
        var employees = await context.Set<Employee>()
            .ToListAsync();

        // Explicit ordering takes precedence
        var employeesByName = await context.Set<Employee>()
            .OrderBy(_ => _.Name)
            .ToListAsync();

        #endregion
    }

    static async Task IncludeSupport()
    {
        DbContext context = null!;

        #region IncludeSupport

        // Departments ordered by DisplayOrder
        // Employees ordered by HireDate, then Salary descending
        var departments = await context.Set<Department>()
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
