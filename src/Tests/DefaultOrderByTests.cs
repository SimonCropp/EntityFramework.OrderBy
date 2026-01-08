[TestFixture]
public class DefaultOrderByTests
{
    [Test]
    public async Task QueryWithoutOrderBy_AppliesDefaultDescendingOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities.ToListAsync();

        // Should be ordered by CreatedDate descending (newest first)
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));  // 2024-03-10
        Assert.That(results[2].Name, Is.EqualTo("Alpha"));  // 2024-01-01
        await Verify(results);
    }

    [Test]
    public async Task QueryWithoutOrderBy_AppliesDefaultAscendingOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.AnotherEntities.ToListAsync();

        // Should be ordered by Name ascending
        Assert.That(results[0].Name, Is.EqualTo("Apple"));
        Assert.That(results[1].Name, Is.EqualTo("Mango"));
        Assert.That(results[2].Name, Is.EqualTo("Zebra"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithExplicitOrderBy_DoesNotApplyDefault()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities
            .OrderBy(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Alpha"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Gamma"));
        await Verify(results);
    }

    [Test]
    public async Task EntityWithoutConfiguration_NoDefaultOrderApplied()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // Should work without throwing - no ordering guaranteed
        var results = await context.EntitiesWithoutDefaultOrder.ToListAsync();

        Assert.That(results, Has.Count.EqualTo(3));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithWhere_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities
            .Where(_ => _.Name != "Alpha")
            .ToListAsync();

        // Should still apply default ordering
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));  // 2024-03-10
        await Verify(results);
    }

    [Test]
    public async Task QueryWithMultipleOrderings_AppliesAllInOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.EntitiesWithMultipleOrderings.ToListAsync();

        // Expected order: Category ASC, then Priority DESC, then Name ASC
        // A, 2, Item1
        // A, 2, Item2
        // A, 1, Item3
        // B, 2, Item4
        // B, 1, Item1
        Assert.That(results, Has.Count.EqualTo(5));
        Assert.That(results[0].Category, Is.EqualTo("A"));
        Assert.That(results[0].Priority, Is.EqualTo(2));
        Assert.That(results[0].Name, Is.EqualTo("Item1"));

        Assert.That(results[1].Category, Is.EqualTo("A"));
        Assert.That(results[1].Priority, Is.EqualTo(2));
        Assert.That(results[1].Name, Is.EqualTo("Item2"));

        Assert.That(results[2].Category, Is.EqualTo("A"));
        Assert.That(results[2].Priority, Is.EqualTo(1));

        Assert.That(results[3].Category, Is.EqualTo("B"));
        Assert.That(results[3].Priority, Is.EqualTo(2));

        Assert.That(results[4].Category, Is.EqualTo("B"));
        Assert.That(results[4].Priority, Is.EqualTo(1));
        await Verify(results);
    }

    [Test]
    public async Task IncludeWithoutExplicitOrdering_AppliesDefaultOrderingToNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees)
            .ToListAsync();

        // Departments should be ordered by DisplayOrder (1, 2, 3)
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));
        Assert.That(results[1].Name, Is.EqualTo("Sales"));
        Assert.That(results[2].Name, Is.EqualTo("HR"));

        // Employees in Engineering should be ordered by HireDate descending (newest first)
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees, Has.Count.EqualTo(3));
        Assert.That(engEmployees[0].Name, Is.EqualTo("Bob"));      // 2024-03-20
        Assert.That(engEmployees[1].Name, Is.EqualTo("Alice"));    // 2024-01-15
        Assert.That(engEmployees[2].Name, Is.EqualTo("Charlie"));  // 2023-06-10

        // Employees in Sales should be ordered by HireDate descending
        var salesEmployees = results[1].Employees;
        Assert.That(salesEmployees, Has.Count.EqualTo(2));
        Assert.That(salesEmployees[0].Name, Is.EqualTo("Diana"));  // 2024-02-05
        Assert.That(salesEmployees[1].Name, Is.EqualTo("Eve"));    // 2023-11-01
        await Verify(results);
    }

    [Test]
    public async Task IncludeWithExplicitOrdering_DoesNotApplyDefaultToNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees.OrderBy(_ => _.Name))
            .ToListAsync();

        // Departments should be ordered by DisplayOrder (default)
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));

        // Employees should be ordered by Name (explicit), not HireDate (default)
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees, Has.Count.EqualTo(3));
        Assert.That(engEmployees[0].Name, Is.EqualTo("Alice"));
        Assert.That(engEmployees[1].Name, Is.EqualTo("Bob"));
        Assert.That(engEmployees[2].Name, Is.EqualTo("Charlie"));
        await Verify(results);
    }

    [Test]
    public async Task ParentQueryWithoutOrderBy_AppliesDefaultToParentOnly()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // Query departments without Include - should apply default ordering
        var results = await context.Departments.ToListAsync();

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));  // DisplayOrder 1
        Assert.That(results[1].Name, Is.EqualTo("Sales"));        // DisplayOrder 2
        Assert.That(results[2].Name, Is.EqualTo("HR"));           // DisplayOrder 3
        await Verify(results);
    }

    [Test]
    public async Task ParentQueryWithExplicitOrderBy_DoesNotApplyDefault()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees)
            .OrderByDescending(_ => _.Name)
            .ToListAsync();

        // Departments should be ordered by Name descending (explicit), not DisplayOrder (default)
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Sales"));
        Assert.That(results[1].Name, Is.EqualTo("HR"));
        Assert.That(results[2].Name, Is.EqualTo("Engineering"));

        // Nested employees should still get default ordering (HireDate descending)
        var salesEmployees = results[0].Employees;
        Assert.That(salesEmployees[0].Name, Is.EqualTo("Diana"));  // 2024-02-05
        Assert.That(salesEmployees[1].Name, Is.EqualTo("Eve"));    // 2023-11-01
        await Verify(results);
    }

    [Test]
    public async Task QueryWithOrderByDescending_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities
            .OrderByDescending(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name descending (explicit), not CreatedDate descending (default)
        Assert.That(results[0].Name, Is.EqualTo("Gamma"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Alpha"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithOrderByThenBy_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Add some test data with same CreatedDate to test ThenBy
        context.TestEntities.Add(new() { Name = "Delta", CreatedDate = DateTime.Parse("2024-06-15") });
        await context.SaveChangesAsync();

        Recording.Start();
        var results = await context.TestEntities
            .OrderBy(_ => _.CreatedDate)
            .ThenBy(_ => _.Name)
            .ToListAsync();

        // Should use explicit ordering, not default
        var betaDelta = results.Where(_ => _.CreatedDate == DateTime.Parse("2024-06-15")).ToList();
        Assert.That(betaDelta[0].Name, Is.EqualTo("Beta"));
        Assert.That(betaDelta[1].Name, Is.EqualTo("Delta"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithWhereAndExplicitOrderBy_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities
            .Where(_ => _.Name != "Alpha")
            .OrderBy(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Beta"));
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithSelectAndExplicitOrderBy_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities
            .OrderBy(_ => _.Name)
            .Select(_ => new { _.Name, _.CreatedDate })
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Alpha"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Gamma"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithThenByDescending_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.EntitiesWithMultipleOrderings
            .OrderBy(_ => _.Category)
            .ThenByDescending(_ => _.Name)
            .ToListAsync();

        // Should use explicit ordering (Category ASC, Name DESC), not default
        Assert.That(results[0].Category, Is.EqualTo("A"));
        Assert.That(results[0].Name, Is.EqualTo("Item3"));

        Assert.That(results[1].Category, Is.EqualTo("A"));
        Assert.That(results[1].Name, Is.EqualTo("Item2"));

        Assert.That(results[2].Category, Is.EqualTo("A"));
        Assert.That(results[2].Name, Is.EqualTo("Item1"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithMultipleExplicitOrderings_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .OrderBy(_ => _.Name)
            .ThenByDescending(_ => _.DisplayOrder)
            .ToListAsync();

        // Should use explicit ordering (Name ASC, DisplayOrder DESC), not default (DisplayOrder ASC)
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));
        Assert.That(results[1].Name, Is.EqualTo("HR"));
        Assert.That(results[2].Name, Is.EqualTo("Sales"));
        await Verify(results);
    }

    [Test]
    public async Task IncludeWithExplicitOrderByDescending_IgnoresDefaultForNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees.OrderByDescending(_ => _.Name))
            .ToListAsync();

        // Employees should be ordered by Name descending (explicit), not HireDate descending (default)
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees[0].Name, Is.EqualTo("Charlie"));
        Assert.That(engEmployees[1].Name, Is.EqualTo("Bob"));
        Assert.That(engEmployees[2].Name, Is.EqualTo("Alice"));
        await Verify(results);
    }

    [Test]
    public async Task IncludeWithExplicitThenBy_IgnoresDefaultForNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees.OrderBy(_ => _.Salary).ThenBy(_ => _.Name))
            .ToListAsync();

        // Employees should use explicit ordering (Salary ASC, Name ASC), not default (HireDate DESC)
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees, Has.Count.EqualTo(3));

        // Verify they're ordered by Salary first, then Name
        Assert.That(engEmployees[0].Salary, Is.LessThanOrEqualTo(engEmployees[1].Salary));
        Assert.That(engEmployees[1].Salary, Is.LessThanOrEqualTo(engEmployees[2].Salary));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithWhereNullComparison_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Add test data with null properties
        context.TestEntities.Add(new() { Name = "NullProperty", CreatedDate = DateTime.Parse("2024-07-01") });
        await context.SaveChangesAsync();

        Recording.Start();
        // This query should be translatable to SQL
        var results = await context.TestEntities
            .Where(_ => string.Equals(_.Name, null))
            .ToListAsync();

        // Should apply default ordering (CreatedDate DESC) and translate properly
        Assert.That(results, Is.Empty);
        await Verify(results);
    }

    [Test]
    public async Task QueryWithWhereNotNullComparison_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // This query should be translatable to SQL
        var results = await context.TestEntities
            .Where(_ => _.Name != "")
            .ToListAsync();

        // Should apply default ordering (CreatedDate DESC) and translate properly
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Beta"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithComplexWhere_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // Complex where clause with default ordering
        var results = await context.TestEntities
            .Where(_ => _.Name.StartsWith('A') || _.Name.Contains("eta"))
            .ToListAsync();

        // Should apply default ordering (CreatedDate DESC)
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Alpha"));  // 2024-01-01
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WorksWithDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // ToQueryString should not throw - expression must be translatable
        var query = context.TestEntities.Where(_ => _.Name != "");
        var sql = query.ToQueryString();

        Assert.That(sql, Does.Contain("ORDER BY"));
        Assert.That(sql, Does.Contain("CreatedDate"));
    }

    [Test]
    public async Task ToQueryString_WorksWithWhereAndDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Complex query with Where and default ordering
        var query = context.TestEntities
            .Where(_ => string.Equals(_.Name, "Alpha"));
        var sql = query.ToQueryString();

        Assert.That(sql, Does.Contain("ORDER BY"));
        Assert.That(sql, Does.Contain("WHERE"));
    }

    [Test]
    public async Task QueryWithMultipleWhereConditions_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities
            .Where(_ => _.Name != "")
            .Where(_ => _.CreatedDate > DateTime.Parse("2024-02-01"))
            .ToListAsync();

        // Should apply default ordering (CreatedDate DESC)
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Name, Is.EqualTo("Beta"));
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));
        await Verify(results);
    }

    [Test]
    public async Task IncludeWithWhereOnParent_AppliesOrderingToNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Where(_ => _.Name != "")
            .Include(_ => _.Employees)
            .ToListAsync();

        // Should apply ordering to both parent and nested collections
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));

        var engEmployees = results[0].Employees;
        Assert.That(engEmployees[0].Name, Is.EqualTo("Bob"));
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WithNullableStringProperty()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // This reproduces the GraphQL scenario with nullable string properties
        var query = context.TestEntities
            .Where(_ => string.Equals(_.Name, null));

        var sql = query.ToQueryString();
        Assert.That(sql, Does.Contain("WHERE"));
    }

    [Test]
    public async Task QueryWithSelectProjection_AppliesOrderingBeforeSelect()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // Simulate GraphQL-style projection that only selects specific fields
        var results = await context.TestEntities
            .Select(_ => new TestEntity { Id = _.Id, Name = _.Name })
            .ToListAsync();

        // Should be ordered by CreatedDate descending (default ordering applied before Select)
        // Even though CreatedDate is not in the projection
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Beta"));
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));
        Assert.That(results[2].Name, Is.EqualTo("Alpha"));
        await Verify(results);
    }

    [Test]
    public async Task QueryWithWhereAndSelectProjection_AppliesOrderingBeforeSelect()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // This reproduces the exact GraphQL.EntityFramework scenario:
        // Where clause followed by Select projection
        var results = await context.TestEntities
            .Where(_ => _.Name != "")
            .Select(_ => new TestEntity { Id = _.Id, Name = _.Name })
            .ToListAsync();

        // Should be ordered by CreatedDate descending (applied before the Select)
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));  // 2024-03-10
        Assert.That(results[2].Name, Is.EqualTo("Alpha"));  // 2024-01-01
        await Verify(results);
    }

    [Test]
    public async Task QueryWithNullComparisonAndSelectProjection_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // This is the exact failing scenario from GraphQL.EntityFramework
        // Where with null comparison + Select projection
        var results = await context.TestEntities
            .Where(_ => string.Equals(_.Name, null))
            .Select(_ => new TestEntity { Id = _.Id })
            .ToListAsync();

        // The query should translate successfully without throwing
        // "OrderBy(p => new TestEntity{ Id = p.Id }.Property)" error
        Assert.That(results, Is.Empty);
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WithSelectProjection_ShowsOrderByBeforeSelect()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Verify the SQL shows ORDER BY is applied correctly
        var query = context.TestEntities
            .Where(_ => _.Name != "")
            .Select(_ => new TestEntity { Id = _.Id, Name = _.Name });

        var sql = query.ToQueryString();

        // SQL should contain ORDER BY and it should work correctly
        Assert.That(sql, Does.Contain("ORDER BY"));
        Assert.That(sql, Does.Contain("CreatedDate"));
    }

    [Test]
    public async Task QueryWithSelectProjectionOnlyId_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // Select only the Id field (like GraphQL does)
        // Default ordering by CreatedDate should still work
        var results = await context.TestEntities
            .Select(_ => new TestEntity { Id = _.Id })
            .ToListAsync();

        Assert.That(results, Has.Count.EqualTo(3));
        // All should have Ids, ordered by CreatedDate descending
        Assert.That(results.All(_ => _.Id > 0), Is.True);
        await Verify(results);
    }

    [Test]
    public async Task QueryWithComplexWhereAndSelectProjection_CanBeTranslated()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // Complex scenario: complex where + projection
        var results = await context.TestEntities
            .Where(_ => _.Name.StartsWith('A') || _.Name.Contains("eta"))
            .Select(_ => new TestEntity { Id = _.Id, Name = _.Name })
            .ToListAsync();

        // Should be ordered by CreatedDate descending
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Alpha"));  // 2024-01-01
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WithWhereNullComparisonAndSelectProjection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // This exact scenario was failing in GraphQL.EntityFramework
        var query = context.TestEntities
            .Where(_ => string.Equals(_.Name, null))
            .Select(_ => new TestEntity { Id = _.Id });

        // Should not throw translation error
        var sql = query.ToQueryString();

        Assert.That(sql, Does.Contain("WHERE"));
        Assert.That(sql, Does.Contain("ORDER BY"));
    }

    [Test]
    public async Task IncludeCollectionNavigation_EfCoreAddsParentIdToOrderBy()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        // When querying with Include for collection navigations,
        // EF Core automatically adds parent ID to ORDER BY
        // This is required for proper materialization of parent-child relationships
        var results = await context.Departments
            .Include(_ => _.Employees)
            .ToListAsync();

        // Verify results are correct
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));  // DisplayOrder 1
        Assert.That(results[1].Name, Is.EqualTo("Sales"));        // DisplayOrder 2
        Assert.That(results[2].Name, Is.EqualTo("HR"));           // DisplayOrder 3

        // Verify nested collections have employees
        Assert.That(results[0].Employees, Has.Count.EqualTo(3));
        Assert.That(results[1].Employees, Has.Count.EqualTo(2));
        Assert.That(results[2].Employees, Has.Count.EqualTo(1));

        // The generated SQL will show:
        // ORDER BY d.Id, e.HireDate desc
        // where d.Id is added by EF Core (not by this library)
        // and e.HireDate desc comes from the configured default ordering
        await Verify(results);
    }

    [Test]
    public async Task ExplicitOrderByWithInclude_WorksCorrectly()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();

        // When you add explicit OrderBy before Include,
        // EF Core preserves your ordering and adds parent.Id after it

        var query = context.Departments
            .OrderByDescending(_ => _.DisplayOrder)  // Explicit ordering
            .Include(_ => _.Employees)
            .AsQueryable();

        var sql = query.ToQueryString();
        var results = await query.ToListAsync();

        // SQL should have: ORDER BY DisplayOrder DESC, d.Id, e.HireDate DESC
        Assert.That(sql, Does.Contain("DisplayOrder"));
        Assert.That(sql, Does.Contain("DESC"));

        // Results ordered by DisplayOrder descending
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].DisplayOrder, Is.EqualTo(3));  // HR
        Assert.That(results[1].DisplayOrder, Is.EqualTo(2));  // Sales
        Assert.That(results[2].DisplayOrder, Is.EqualTo(1));  // Engineering

        // Employee collections properly populated
        Assert.That(results[0].Employees, Has.Count.EqualTo(1)); // HR
        Assert.That(results[1].Employees, Has.Count.EqualTo(2)); // Sales
        Assert.That(results[2].Employees, Has.Count.EqualTo(3)); // Engineering

        await Verify(new
        {
            sql,
            departmentOrder = results.Select(_ => _.Name).ToArray(),
            employeeCounts = results.Select(_ => _.Employees.Count).ToArray()
        });
    }

    [Test]
    public async Task DefaultOrderingWithInclude_BothParentAndChildOrderingsApplied()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();

        // When both parent and child have default orderings configured:
        // - Parent: OrderBy DisplayOrder (from config)
        // - Child: OrderByDescending HireDate (from config)
        // And we use Include for the collection navigation

        var results = await context.Departments
            .Include(_ => _.Employees)
            .ToListAsync();

        // Parent ordering is applied: DisplayOrder ascending
        Assert.That(results[0].DisplayOrder, Is.EqualTo(1));  // Engineering
        Assert.That(results[1].DisplayOrder, Is.EqualTo(2));  // Sales
        Assert.That(results[2].DisplayOrder, Is.EqualTo(3));  // HR

        // Child ordering is applied within each parent: HireDate descending
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees[0].HireDate, Is.EqualTo(new DateTime(2024, 3, 20)));  // Bob (newest)
        Assert.That(engEmployees[1].HireDate, Is.EqualTo(new DateTime(2024, 1, 15)));  // Alice
        Assert.That(engEmployees[2].HireDate, Is.EqualTo(new DateTime(2023, 6, 10)));  // Charlie (oldest)

        // The SQL will have: ORDER BY d.DisplayOrder, d.Id, e.HireDate DESC
        // Where:
        // - d.DisplayOrder comes from default ordering config
        // - d.Id is added by EF Core (critical for materialization)
        // - e.HireDate DESC comes from default ordering config

        await Verify(results);
    }

    [Test]
    public async Task DefaultOrdering_IsPreservedWithInclude()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();

        // Department has default ordering configured: OrderBy DisplayOrder
        // When we Include employees, the default ordering should be preserved
        var query = context.Departments
            .Include(_ => _.Employees);

        var sql = query.ToQueryString();
        var results = await query.ToListAsync();

        // FIXED: The SQL now shows ORDER BY d.DisplayOrder, d.Id, e.HireDate DESC
        // The default ordering (DisplayOrder) is PRESERVED!
        // EF Core adds d.Id after it for materialization, but doesn't replace it

        // Results are ordered by DisplayOrder (configured default), NOT just by Id
        Assert.That(results[0].DisplayOrder, Is.EqualTo(1));  // Engineering
        Assert.That(results[1].DisplayOrder, Is.EqualTo(2));  // Sales
        Assert.That(results[2].DisplayOrder, Is.EqualTo(3));  // HR

        // Verify the SQL contains both DisplayOrder and Id in ORDER BY
        Assert.That(sql, Does.Contain("ORDER BY"));
        Assert.That(sql, Does.Contain("DisplayOrder"));
        Assert.That(sql, Does.Contain("Id"));

        await Verify(new
        {
            sql,
            sqlContainsDisplayOrder = sql.Contains("DisplayOrder"),
            sqlContainsId = sql.Contains("Id"),
            orderInResults = results.Select(_ => new { _.Id, _.DisplayOrder, _.Name }).ToArray()
        });
    }

    [Test]
    public async Task DefaultOrdering_WithIncludeAndSelect_NoNavInSelect()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Select that doesn't include the navigation property
        var query = context.Departments
            .Include(_ => _.Employees)
            .Select(_ => new { _.Id, _.Name, _.DisplayOrder });

        var sql = query.ToQueryString();

        await Verify(sql);
    }

    [Test]
    public async Task DefaultOrdering_WithIncludeAndSelect_WithNavInSelect()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Select that includes the navigation property
        var query = context.Departments
            .Include(_ => _.Employees)
            .Select(_ => new { _.Id, _.Name, _.DisplayOrder, _.Employees });

        var sql = query.ToQueryString();

        await Verify(sql);
    }

    [Test]
    public async Task SelectWithOrderByInProjection_AppliesDefaultOrderToParent()
    {
        // This test verifies the fix for: OrderingDetector should skip OrderBy within Select projections
        // For when OrderBy is added to nested collections in Select projections for deterministic ordering
        // This OrderBy should NOT prevent default ordering from being applied to the parent query
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var query = context.Departments
            .Select(_ => new
            {
                _.Id,
                _.Name,
                _.DisplayOrder,
                // OrderBy on nested collection (like GraphQL.EntityFramework does)
                Employees = _.Employees.OrderBy(e => e.Id).ToList()
            });

        var sql = query.ToQueryString();

        // Verify SQL includes default ordering for Department (DisplayOrder)
        // The OrderBy on Employees collection should NOT prevent this
        await Verify(sql);
    }

    [Test]
    public async Task SelectWithOrderByInProjection_AppliesDefaultOrderToParent_ResultsVerification()
    {
        // Runtime verification that the fix works correctly with actual query execution
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Select(_ => new
            {
                _.Id,
                _.Name,
                _.DisplayOrder,
                // OrderBy on nested collection (like GraphQL.EntityFramework does)
                Employees = _.Employees.OrderBy(e => e.Id).ToList()
            })
            .ToListAsync();

        // Should be ordered by DisplayOrder (default), not affected by the OrderBy in the projection
        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));  // DisplayOrder 1
        Assert.That(results[1].Name, Is.EqualTo("Sales"));        // DisplayOrder 2
        Assert.That(results[2].Name, Is.EqualTo("HR"));           // DisplayOrder 3

        // Verify the nested employees are ordered by Id (from the Select projection)
        Assert.That(results[0].Employees[0].Id, Is.LessThan(results[0].Employees[1].Id));

        await Verify(results);
    }

}
