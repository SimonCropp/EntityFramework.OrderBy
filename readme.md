# <img src="/src/icon.png" height="30px"> EntityFramework.OrderBy

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/entityframework-orderby)](https://ci.appveyor.com/project/SimonCropp/entityframework-orderby)
[![NuGet Status](https://img.shields.io/nuget/v/EfOrderBy.svg)](https://www.nuget.org/packages/EfOrderBy/)

**See [Milestones](../../milestones?state=closed) for release notes.**

Applies default ordering to Entity Framework Core queries based on fluent configuration. This ensures consistent query results and prevents non-deterministic ordering issues.

## NuGet package

https://nuget.org/packages/EfOrderBy/


## Features

- **Automatic ordering**: Queries without explicit `OrderBy` automatically use configured default ordering
- **Include() support**: Nested collections in `.Include()` expressions are automatically ordered
- **Fluent configuration**: Configure default ordering using the familiar EF Core fluent API
- **Multi-column ordering**: Chain multiple ordering clauses with `ThenBy` and `ThenByDescending`
- **Validation mode**: Optionally require all entities to have default ordering configured


## Usage


### 1. Enable the interceptor

Configure the default ordering interceptor in the `DbContext`:

<!-- snippet: EnableInterceptor -->
<a id='snippet-EnableInterceptor'></a>
```cs
protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
    builder.UseDefaultOrderBy();
```
<sup><a href='/src/Tests/Snippets.cs#L9-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableInterceptor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### 2. Configure entity ordering

Use the fluent API to configure default ordering for entities:

<!-- snippet: ConfigureOrdering -->
<a id='snippet-ConfigureOrdering'></a>
```cs
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Employee>()
        .OrderBy(_ => _.HireDate)
        .ThenByDescending(_ => _.Salary);

    builder.Entity<Department>()
        .OrderBy(_ => _.DisplayOrder);
}
```
<sup><a href='/src/Tests/Snippets.cs#L19-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### 3. Query without explicit OrderBy

Queries without explicit ordering automatically use the configured default:

<!-- snippet: QueryWithoutOrderBy -->
<a id='snippet-QueryWithoutOrderBy'></a>
```cs
// Automatically ordered by HireDate, then Salary descending
var employees = await context.Set<Employee>()
    .ToListAsync();

// Explicit ordering takes precedence
var employeesByName = await context.Set<Employee>()
    .OrderBy(_ => _.Name)
    .ToListAsync();
```
<sup><a href='/src/Tests/Snippets.cs#L51-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-QueryWithoutOrderBy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Include() Support

Nested collections in `.Include()` expressions are automatically ordered:

<!-- snippet: IncludeSupport -->
<a id='snippet-IncludeSupport'></a>
```cs
// Departments ordered by DisplayOrder
// Employees ordered by HireDate, then Salary descending
var departments = await context.Set<Department>()
    .Include(_ => _.Employees)
    .ToListAsync();
```
<sup><a href='/src/Tests/Snippets.cs#L69-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeSupport' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Multi-Column Ordering

Chain multiple ordering clauses using `ThenBy` and `ThenByDescending`:

<!-- snippet: MultiColumnOrdering -->
<a id='snippet-MultiColumnOrdering'></a>
```cs
builder.Entity<Product>()
    .OrderBy(_ => _.Category)
    .ThenBy(_ => _.Name)
    .ThenByDescending(_ => _.Price);
```
<sup><a href='/src/Tests/Snippets.cs#L82-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-MultiColumnOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Require Ordering for All Entities

Enable validation mode to ensure all entities have default ordering configured:

<!-- snippet: RequireOrdering -->
<a id='snippet-RequireOrdering'></a>
```cs
protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
    builder.UseDefaultOrderBy(
        requireOrderingForAllEntities: true);
```
<sup><a href='/src/Tests/Snippets.cs#L36-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-RequireOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This throws an exception during the first query if any entity type lacks default ordering configuration:

```
Default ordering is required for all entity types but the following entities
do not have ordering configured: Product, Customer.
Use modelBuilder.Entity<T>().OrderBy() to configure default ordering.
```

Validation occurs once per `DbContext` type for performance.


## Example

<!-- snippet: CompleteExample -->
<a id='snippet-CompleteExample'></a>
```cs
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
```
<sup><a href='/src/Tests/Snippets.cs#L93-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-CompleteExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Icon

[Russian Dolls](https://thenounproject.com/icon/russian-dolls-4020530/) designed by [Edit Pongrácz](https://thenounproject.com/creator/pongraczeditdodo/) from [The Noun Project](https://thenounproject.com)
