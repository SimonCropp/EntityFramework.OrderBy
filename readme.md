# <img src="/src/icon.png" height="30px"> EntityFramework.OrderBy

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/entityframework-orderby)](https://ci.appveyor.com/project/SimonCropp/entityframework-orderby)
[![NuGet Status](https://img.shields.io/nuget/v/EntityFramework.OrderBy.svg)](https://www.nuget.org/packages/EntityFramework.OrderBy/)

**See [Milestones](../../milestones?state=closed) for release notes.**


## NuGet package

https://nuget.org/packages/EntityFramework.OrderBy/


## Overview

EntityFramework.OrderBy automatically applies default ordering to Entity Framework Core queries based on fluent configuration. This ensures consistent query results and prevents non-deterministic ordering issues.

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
protected override void OnConfiguring(
    DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseDefaultOrderBy();
}
```
<sup><a href='/src/Tests/Snippets.cs#L4-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableInterceptor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### 2. Configure entity ordering

Use the fluent API to configure default ordering for entities:

<!-- snippet: ConfigureOrdering -->
<a id='snippet-ConfigureOrdering'></a>
```cs
protected override void OnModelCreating(
    ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Employee>()
        .OrderBy(e => e.HireDate)
        .ThenByDescending(e => e.Salary);

    modelBuilder.Entity<Department>()
        .OrderBy(d => d.DisplayOrder);
}
```
<sup><a href='/src/Tests/Snippets.cs#L14-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureOrdering' title='Start of snippet'>anchor</a></sup>
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
    .OrderBy(e => e.Name)
    .ToListAsync();
```
<sup><a href='/src/Tests/Snippets.cs#L35-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-QueryWithoutOrderBy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Include() Support

Nested collections in `.Include()` expressions are automatically ordered:

<!-- snippet: IncludeSupport -->
<a id='snippet-IncludeSupport'></a>
```cs
// Departments ordered by DisplayOrder
// Employees ordered by HireDate, then Salary descending
var departments = await context.Set<Department>()
    .Include(d => d.Employees)
    .ToListAsync();
```
<sup><a href='/src/Tests/Snippets.cs#L53-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeSupport' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Multi-Column Ordering

Chain multiple ordering clauses using `ThenBy` and `ThenByDescending`:

<!-- snippet: MultiColumnOrdering -->
<a id='snippet-MultiColumnOrdering'></a>
```cs
modelBuilder.Entity<Product>()
    .OrderBy(p => p.Category)
    .ThenBy(p => p.Name)
    .ThenByDescending(p => p.Price);
```
<sup><a href='/src/Tests/Snippets.cs#L66-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-MultiColumnOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Require Ordering for All Entities

Enable validation mode to ensure all entities have default ordering configured:

<!-- snippet: RequireOrdering -->
<a id='snippet-RequireOrdering'></a>
```cs
protected override void OnConfiguring(
    DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseDefaultOrderBy(
        requireOrderingForAllEntities: true);
}
```
<sup><a href='/src/Tests/Snippets.cs#L77-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-RequireOrdering' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/Tests/Snippets.cs#L88-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-CompleteExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->



## Icon

[Russian Dolls](https://thenounproject.com/icon/russian-dolls-4020530/) designed by [Edit Pongrácz](https://thenounproject.com/creator/pongraczeditdodo/) from [The Noun Project](https://thenounproject.com)
