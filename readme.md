# <img src="/src/icon.png" height="30px"> EntityFramework.OrderBy

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/entityframework-orderby)](https://ci.appveyor.com/project/SimonCropp/entityframework-orderby)
[![NuGet Status](https://img.shields.io/nuget/v/EfOrderBy.svg)](https://www.nuget.org/packages/EfOrderBy/)

**See [Milestones](../../milestones?state=closed) for release notes.**

Applies default ordering to EntityFramework queries based on fluent configuration. This ensures consistent query results and prevents non-deterministic ordering issues.

## NuGet package

https://nuget.org/packages/EfOrderBy/


## Features

- **Automatic ordering**: Queries without explicit `OrderBy` automatically use configured default ordering
- **Include() support**: Nested collections in `.Include()` expressions are automatically ordered
- **Deterministic paging**: Ordering is applied before `Skip`/`Take`/`First`, so pages and single results are stable
- **Inheritance support**: Ordering configured on a base entity type is automatically inherited by derived types (TPH)
- **Fluent configuration**: Configure default ordering using the familiar EF Core fluent API
- **Multi-column ordering**: Chain multiple ordering clauses with `ThenBy` and `ThenByDescending`
- **Automatic indexes**: Database indexes are automatically created for ordering columns
- **Validation mode**: Optionally require all entities to have default ordering configured
- **Redundant ordering detection**: Optionally throw when a query explicitly applies the same ordering as the configured default


## Usage


### 1. Enable the interceptor

Configure the default ordering interceptor in the `DbContext`:

<!-- snippet: EnableInterceptor -->
<a id='snippet-EnableInterceptor'></a>
```cs
protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
    builder.UseDefaultOrderBy();
```
<sup><a href='/src/Tests/Snippets.cs#L7-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableInterceptor' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Tests/Snippets.cs#L17-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### 3. Query without explicit OrderBy

Queries without explicit ordering automatically use the configured default:

<!-- snippet: QueryWithoutOrderBy -->
<a id='snippet-QueryWithoutOrderBy'></a>
```cs
// Automatically ordered by HireDate, then Salary descending
var employees = await context.Employees
    .ToListAsync();

// Explicit ordering takes precedence
var employeesByName = await context.Employees
    .OrderBy(_ => _.Name)
    .ToListAsync();
```
<sup><a href='/src/Tests/Snippets.cs#L71-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-QueryWithoutOrderBy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Paging and Single Results

The default ordering is applied *before* any operator that chooses which rows come back, so
`Skip`, `Take`, `First`, `FirstOrDefault`, `Last`, `LastOrDefault` and `ElementAt` all select
from an ordered sequence:

<!-- snippet: PagingAndSingleResults -->
<a id='snippet-PagingAndSingleResults'></a>
```cs
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
```
<sup><a href='/src/Tests/Snippets.cs#L89-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-PagingAndSingleResults' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without this, a page would be an arbitrary set of rows that happens to be sorted, and pages
could both skip and repeat rows.

Aggregates such as `Count` and `Any`, and `Single`, are left alone, since ordering them is
pointless work.


## Include() Support

Nested collections in `.Include()` expressions are automatically ordered:

<!-- snippet: IncludeSupport -->
<a id='snippet-IncludeSupport'></a>
```cs
// Departments ordered by DisplayOrder
// Employees ordered by HireDate, then Salary descending
var departments = await context.Departments
    .Include(_ => _.Employees)
    .ToListAsync();
```
<sup><a href='/src/Tests/Snippets.cs#L110-L118' title='Snippet source file'>snippet source</a> | <a href='#snippet-IncludeSupport' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Inheritance Support

When using TPH (Table Per Hierarchy) inheritance, ordering configured on a base entity type is automatically inherited by derived types. This eliminates the need to duplicate `.OrderBy()` on every derived type.

<!-- snippet: InheritanceOrdering -->
<a id='snippet-InheritanceOrdering'></a>
```cs
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
```
<sup><a href='/src/Tests/Snippets.cs#L186-L231' title='Snippet source file'>snippet source</a> | <a href='#snippet-InheritanceOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Behavior:

- `context.DerivedEntitiesA.ToListAsync()` is ordered by `SortOrder` (inherited from `BaseEntity`)
- `context.DerivedEntitiesB.ToListAsync()` is ordered by `Name` descending (its own explicit configuration)
- Derived types with their own `.OrderBy()` take precedence over the base type's ordering
- Inherited orderings do not create duplicate database indexes (the base type's index covers the same columns)


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
<sup><a href='/src/Tests/Snippets.cs#L123-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-MultiColumnOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Automatic Index Creation

When configuring default ordering, a database index is automatically created for the ordering columns. This improves query performance since the database can use the index when sorting.

```cs
builder.Entity<Product>()
    .OrderBy(_ => _.Category)
    .ThenBy(_ => _.Name)
    .ThenByDescending(_ => _.Price);

// Automatically creates index: IX_Product_DefaultOrder (Category, Name, Price)
```

The index:

- Is named `IX_{EntityName}_DefaultOrder`
- Contains all columns in the ordering chain as a composite index
- Is automatically updated when using `ThenBy`/`ThenByDescending`

This eliminates the need to manually create indexes that match the ordering configuration.


### Custom Index Names

The auto-generated index name must not exceed 128 characters (SQL Server limit). If an entity name is too long, use `WithIndexName` to specify a custom index name:

```cs
builder.Entity<EntityWithVeryLongNameThatWouldExceedTheLimit>()
    .OrderBy(_ => _.Name)
    .WithIndexName("IX_LongEntity_Order");
```

If the auto-generated name exceeds 128 characters, an `Exception` is thrown with a message suggesting to use `WithIndexName()`.


### String Column Indexes

Some database providers silently cap string column lengths when an index is added. For example, [SQL Server limits index keys to 900 bytes](https://github.com/dotnet/efcore/issues/31167), so EF Core's SQL Server provider automatically reduces `nvarchar` columns to 450 characters when indexed.

To prevent this unexpected column modification, automatic index creation is skipped for `string` properties that have no `MaxLength` configured or a `MaxLength` exceeding the provider's limit. The limit is determined by querying the provider's `RelationalTypeMappingSource`, so it automatically adapts to any database engine.

To include a string column in the automatic index, configure a `MaxLength` within the provider's limit:

```cs
builder.Entity<Product>()
    .Property(_ => _.Category)
    .HasMaxLength(450);

builder.Entity<Product>()
    .OrderBy(_ => _.Category);

// Index is created because Category has MaxLength ≤ 450
```

If the `MaxLength` is not configured or exceeds the limit, the ordering still works — only the automatic index is skipped:

```cs
builder.Entity<Product>()
    .OrderBy(_ => _.Category);

// No index created (Category has no MaxLength), but ordering is still applied to queries
```

For composite indexes, if any string column exceeds the limit, the entire index is skipped.

Providers without a string index size limit (e.g. PostgreSQL, SQLite) always create the index regardless of `MaxLength`.


### Disabling Index Creation

To opt out of automatic index creation (for example, if indexes are managed separately):

<!-- snippet: DisableIndexCreation -->
<a id='snippet-DisableIndexCreation'></a>
```cs
protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
    builder.UseDefaultOrderBy(
        createIndexes: false);
```
<sup><a href='/src/Tests/Snippets.cs#L56-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-DisableIndexCreation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When index creation is disabled, calling `WithIndexName()` throws an `Exception`.


## Require Ordering for All Entities

Enable validation mode to ensure all entities have default ordering configured:

<!-- snippet: RequireOrdering -->
<a id='snippet-RequireOrdering'></a>
```cs
protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
    builder.UseDefaultOrderBy(
        requireOrderingForAllEntities: true);
```
<sup><a href='/src/Tests/Snippets.cs#L34-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-RequireOrdering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This throws an exception during the first query if any entity type lacks default ordering configuration:

```
Default ordering is required for all entity types but the following entities
do not have ordering configured: Product, Customer.
Use modelBuilder.Entity<T>().OrderBy() to configure default ordering.
```

Validation occurs once per `DbContext` type for performance.


## Detect Redundant Ordering

Explicit ordering in a query takes precedence over the configured default. When that explicit ordering is an exact match for the default, it is redundant. This is usually a leftover from before the default ordering was configured.

Enable detection to throw when a query does this. This is primarily useful in tests:

<!-- snippet: ThrowOnRedundantOrderBy -->
<a id='snippet-ThrowOnRedundantOrderBy'></a>
```cs
protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
    builder.UseDefaultOrderBy(
        throwOnRedundantOrderBy: true);
```
<sup><a href='/src/Tests/Snippets.cs#L45-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-ThrowOnRedundantOrderBy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Given this configuration:

```cs
builder.Entity<Employee>()
    .OrderBy(_ => _.HireDate)
    .ThenByDescending(_ => _.Salary);
```

The following query throws, since it applies exactly the default ordering:

```cs
var employees = await context.Employees
    .OrderBy(_ => _.HireDate)
    .ThenByDescending(_ => _.Salary)
    .ToListAsync();
```

```
The query explicitly orders 'Employee' by OrderBy(HireDate).ThenByDescending(Salary),
which exactly matches the default ordering configured for that entity.
Remove the ordering from the query since it is applied automatically,
or change it to a different ordering.
```

The match must be exact: the same properties, in the same sequence, with the same directions. Anything else is treated as an intentional override and does not throw:

```cs
// Only part of the default ordering
.OrderBy(_ => _.HireDate)

// Different direction
.OrderByDescending(_ => _.HireDate)
.ThenByDescending(_ => _.Salary)

// Additional ordering column
.OrderBy(_ => _.HireDate)
.ThenByDescending(_ => _.Salary)
.ThenBy(_ => _.Name)
```

Ordering of nested collections in `Include()` is checked the same way:

```cs
// Throws, since it matches the default ordering for Employee
var departments = await context.Departments
    .Include(_ => _.Employees
        .OrderBy(employee => employee.HireDate)
        .ThenByDescending(employee => employee.Salary))
    .ToListAsync();
```

Detection happens during query compilation, so it applies to every distinct query the application executes, whether or not the query hits the database.


## Configuration Errors

Calling `OrderBy` or `OrderByDescending` multiple times for the same entity type throws an `Exception`:

```cs
// WRONG - throws Exception
builder.Entity<Employee>()
    .OrderBy(_ => _.HireDate);
builder.Entity<Employee>()
    .OrderBy(_ => _.Salary);  // Error

// CORRECT - use ThenBy for additional columns
builder.Entity<Employee>()
    .OrderBy(_ => _.HireDate)
    .ThenBy(_ => _.Salary);
```

This prevents accidentally overwriting ordering configuration and ensures the intended ordering is applied.


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
<sup><a href='/src/Tests/Snippets.cs#L134-L177' title='Snippet source file'>snippet source</a> | <a href='#snippet-CompleteExample' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Alternative to Verify's OrderEnumerableBy

When using [Verify](https://github.com/VerifyTests/Verify) for snapshot testing, a common pattern is to use [OrderEnumerableBy](https://github.com/VerifyTests/Verify/blob/main/docs/ordering.md#orderenumerableby) to get deterministic ordering of EF entities in snapshots:

```cs
// Verify's OrderEnumerableBy sorts entities during snapshot serialization
VerifierSettings.OrderEnumerableBy<Employee>(_ => _.HireDate);
VerifierSettings.OrderEnumerableBy<Department>(_ => _.DisplayOrder);
```

EntityFramework.OrderBy is an alternative approach. Instead of sorting during serialization, ordering is applied at the database query level. This means queries return deterministic results without needing Verify-specific configuration.

```cs
// EntityFramework.OrderBy applies ordering at the query level
builder.Entity<Employee>()
    .OrderBy(_ => _.HireDate);

builder.Entity<Department>()
    .OrderBy(_ => _.DisplayOrder);
```

Benefits over `OrderEnumerableBy`:

 * Ordering is applied to all queries, not only during snapshot verification
 * Automatic database index creation for ordering columns improves query performance
 * Ordering configuration lives with the entity model rather than in test setup


## Icon

[Russian Dolls](https://thenounproject.com/icon/russian-dolls-4020530/) designed by [Edit Pongrácz](https://thenounproject.com/creator/pongraczeditdodo/) from [The Noun Project](https://thenounproject.com)
