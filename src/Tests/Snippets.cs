// ReSharper disable All
using Microsoft.EntityFrameworkCore;

#region EnableInterceptor

protected override void OnConfiguring(
    DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseDefaultOrderBy();
}

#endregion

#region ConfigureOrdering

protected override void OnModelCreating(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Employee>()
        .OrderBy(e => e.HireDate)
        .ThenByDescending(e => e.Salary);

    modelBuilder.Entity<Department>()
        .OrderBy(d => d.DisplayOrder);
}

#endregion

public class SnippetExamples
{
    async Task QueryWithoutOrderBy()
    {
        DbContext context = null!;

        #region QueryWithoutOrderBy

        // Automatically ordered by HireDate, then Salary descending
        var employees = await context.Set<Employee>()
            .ToListAsync();

        // Explicit ordering takes precedence
        var employeesByName = await context.Set<Employee>()
            .OrderBy(e => e.Name)
            .ToListAsync();

        #endregion
    }

    async Task IncludeSupport()
    {
        DbContext context = null!;

        #region IncludeSupport

        // Departments ordered by DisplayOrder
        // Employees ordered by HireDate, then Salary descending
        var departments = await context.Set<Department>()
            .Include(d => d.Employees)
            .ToListAsync();

        #endregion
    }

    void MultiColumnOrdering(ModelBuilder modelBuilder)
    {
        #region MultiColumnOrdering

        modelBuilder.Entity<Product>()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ThenByDescending(p => p.Price);

        #endregion
    }
}

#region RequireOrdering

protected override void OnConfiguring(
    DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseDefaultOrderBy(
        requireOrderingForAllEntities: true);
}

#endregion

#region CompleteExample

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
    public List<Employee> Employees { get; set; }
}

public class Employee
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; }
    public string Name { get; set; }
    public DateTime HireDate { get; set; }
    public int Salary { get; set; }
}

public class AppDbContext : DbContext
{
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlServer("connection-string")
            .UseDefaultOrderBy();
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>()
            .OrderBy(d => d.DisplayOrder);

        modelBuilder.Entity<Employee>()
            .OrderBy(e => e.HireDate)
            .ThenByDescending(e => e.Salary);
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
}

#endregion

class Product
{
    public string Category { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
