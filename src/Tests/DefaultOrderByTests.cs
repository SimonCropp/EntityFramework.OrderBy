public class DefaultOrderByTests
{
    [Test]
    public async Task Schema()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();
        await Verify(database.Connection);
    }

    [Test]
    public async Task QueryWithoutOrderBy_AppliesDefaultDescendingOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.TestEntities.ToListAsync();

        // Should be ordered by CreatedDate descending (newest first)
        await Assert.That(results[0].Name).IsEqualTo("Beta");   // 2024-06-15
        await Assert.That(results[1].Name).IsEqualTo("Gamma");  // 2024-03-10
        await Assert.That(results[2].Name).IsEqualTo("Alpha");  // 2024-01-01
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
        await Assert.That(results[0].Name).IsEqualTo("Apple");
        await Assert.That(results[1].Name).IsEqualTo("Mango");
        await Assert.That(results[2].Name).IsEqualTo("Zebra");
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
        await Assert.That(results[0].Name).IsEqualTo("Alpha");
        await Assert.That(results[1].Name).IsEqualTo("Beta");
        await Assert.That(results[2].Name).IsEqualTo("Gamma");
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

        await Assert.That(results).Count().IsEqualTo(3);
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
        await Assert.That(results[0].Name).IsEqualTo("Beta");   // 2024-06-15
        await Assert.That(results[1].Name).IsEqualTo("Gamma");  // 2024-03-10
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
        await Assert.That(results).Count().IsEqualTo(5);
        await Assert.That(results[0].Category).IsEqualTo("A");
        await Assert.That(results[0].Priority).IsEqualTo(2);
        await Assert.That(results[0].Name).IsEqualTo("Item1");

        await Assert.That(results[1].Category).IsEqualTo("A");
        await Assert.That(results[1].Priority).IsEqualTo(2);
        await Assert.That(results[1].Name).IsEqualTo("Item2");

        await Assert.That(results[2].Category).IsEqualTo("A");
        await Assert.That(results[2].Priority).IsEqualTo(1);

        await Assert.That(results[3].Category).IsEqualTo("B");
        await Assert.That(results[3].Priority).IsEqualTo(2);

        await Assert.That(results[4].Category).IsEqualTo("B");
        await Assert.That(results[4].Priority).IsEqualTo(1);
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Engineering");
        await Assert.That(results[1].Name).IsEqualTo("Sales");
        await Assert.That(results[2].Name).IsEqualTo("HR");

        // Employees in Engineering should be ordered by HireDate descending (newest first)
        var engEmployees = results[0].Employees;
        await Assert.That(engEmployees).Count().IsEqualTo(3);
        await Assert.That(engEmployees[0].Name).IsEqualTo("Bob");      // 2024-03-20
        await Assert.That(engEmployees[1].Name).IsEqualTo("Alice");    // 2024-01-15
        await Assert.That(engEmployees[2].Name).IsEqualTo("Charlie");  // 2023-06-10

        // Employees in Sales should be ordered by HireDate descending
        var salesEmployees = results[1].Employees;
        await Assert.That(salesEmployees).Count().IsEqualTo(2);
        await Assert.That(salesEmployees[0].Name).IsEqualTo("Diana");  // 2024-02-05
        await Assert.That(salesEmployees[1].Name).IsEqualTo("Eve");    // 2023-11-01
        await Verify(results);
    }

    [Test]
    public async Task ThenInclude_AppliesDefaultOrderingToThirdLevel()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees)
            .ThenInclude(_ => _.Tasks)
            .AsSplitQuery()
            .ToListAsync();

        // Departments ordered by DisplayOrder
        await Assert.That(results[0].Name).IsEqualTo("Engineering");

        // Employees ordered by HireDate descending
        var engEmployees = results[0].Employees;
        await Assert.That(engEmployees[0].Name).IsEqualTo("Bob");

        // Tasks ordered by Priority ascending (via ThenInclude)
        var aliceTasks = engEmployees[1].Tasks; // Alice
        await Assert.That(aliceTasks).Count().IsEqualTo(3);
        await Assert.That(aliceTasks[0].Title).IsEqualTo("Code review"); // Priority 1
        await Assert.That(aliceTasks[1].Title).IsEqualTo("Testing");     // Priority 2
        await Assert.That(aliceTasks[2].Title).IsEqualTo("Design");      // Priority 3

        var bobTasks = engEmployees[0].Tasks; // Bob
        await Assert.That(bobTasks).Count().IsEqualTo(2);
        await Assert.That(bobTasks[0].Title).IsEqualTo("Monitor"); // Priority 1
        await Assert.That(bobTasks[1].Title).IsEqualTo("Deploy");  // Priority 2

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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Engineering");

        // Employees should be ordered by Name (explicit), not HireDate (default)
        var engEmployees = results[0].Employees;
        await Assert.That(engEmployees).Count().IsEqualTo(3);
        await Assert.That(engEmployees[0].Name).IsEqualTo("Alice");
        await Assert.That(engEmployees[1].Name).IsEqualTo("Bob");
        await Assert.That(engEmployees[2].Name).IsEqualTo("Charlie");
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

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Engineering");  // DisplayOrder 1
        await Assert.That(results[1].Name).IsEqualTo("Sales");        // DisplayOrder 2
        await Assert.That(results[2].Name).IsEqualTo("HR");           // DisplayOrder 3
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Sales");
        await Assert.That(results[1].Name).IsEqualTo("HR");
        await Assert.That(results[2].Name).IsEqualTo("Engineering");

        // Nested employees should still get default ordering (HireDate descending)
        var salesEmployees = results[0].Employees;
        await Assert.That(salesEmployees[0].Name).IsEqualTo("Diana");  // 2024-02-05
        await Assert.That(salesEmployees[1].Name).IsEqualTo("Eve");    // 2023-11-01
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
        await Assert.That(results[0].Name).IsEqualTo("Gamma");
        await Assert.That(results[1].Name).IsEqualTo("Beta");
        await Assert.That(results[2].Name).IsEqualTo("Alpha");
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
        await Assert.That(betaDelta[0].Name).IsEqualTo("Beta");
        await Assert.That(betaDelta[1].Name).IsEqualTo("Delta");
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
        await Assert.That(results[0].Name).IsEqualTo("Beta");
        await Assert.That(results[1].Name).IsEqualTo("Gamma");
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
        await Assert.That(results[0].Name).IsEqualTo("Alpha");
        await Assert.That(results[1].Name).IsEqualTo("Beta");
        await Assert.That(results[2].Name).IsEqualTo("Gamma");
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
        await Assert.That(results[0].Category).IsEqualTo("A");
        await Assert.That(results[0].Name).IsEqualTo("Item3");

        await Assert.That(results[1].Category).IsEqualTo("A");
        await Assert.That(results[1].Name).IsEqualTo("Item2");

        await Assert.That(results[2].Category).IsEqualTo("A");
        await Assert.That(results[2].Name).IsEqualTo("Item1");
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
        await Assert.That(results[0].Name).IsEqualTo("Engineering");
        await Assert.That(results[1].Name).IsEqualTo("HR");
        await Assert.That(results[2].Name).IsEqualTo("Sales");
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
        await Assert.That(engEmployees[0].Name).IsEqualTo("Charlie");
        await Assert.That(engEmployees[1].Name).IsEqualTo("Bob");
        await Assert.That(engEmployees[2].Name).IsEqualTo("Alice");
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
        await Assert.That(engEmployees).Count().IsEqualTo(3);

        // Verify they're ordered by Salary first, then Name
        await Assert.That(engEmployees[0].Salary).IsLessThanOrEqualTo(engEmployees[1].Salary);
        await Assert.That(engEmployees[1].Salary).IsLessThanOrEqualTo(engEmployees[2].Salary);
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
        await Assert.That(results).IsEmpty();
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Beta");
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
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].Name).IsEqualTo("Beta");   // 2024-06-15
        await Assert.That(results[1].Name).IsEqualTo("Alpha");  // 2024-01-01
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WorksWithDefaultOrdering()
    {
        await using var context = NewModelOnlyContext();

        // ToQueryString should not throw - expression must be translatable
        var query = context.TestEntities.Where(_ => _.Name != "");
        var sql = query.ToQueryString();

        await Assert.That(sql).Contains("ORDER BY");
        await Assert.That(sql).Contains("CreatedDate");
    }

    [Test]
    public async Task ToQueryString_WorksWithWhereAndDefaultOrdering()
    {
        await using var context = NewModelOnlyContext();

        // Complex query with Where and default ordering
        var query = context.TestEntities
            .Where(_ => string.Equals(_.Name, "Alpha"));
        var sql = query.ToQueryString();

        await Assert.That(sql).Contains("ORDER BY");
        await Assert.That(sql).Contains("WHERE");
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
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].Name).IsEqualTo("Beta");
        await Assert.That(results[1].Name).IsEqualTo("Gamma");
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Engineering");

        var engEmployees = results[0].Employees;
        await Assert.That(engEmployees[0].Name).IsEqualTo("Bob");
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WithNullableStringProperty()
    {
        await using var context = NewModelOnlyContext();

        // This reproduces the GraphQL scenario with nullable string properties
        var query = context.TestEntities
            .Where(_ => string.Equals(_.Name, null));

        var sql = query.ToQueryString();
        await Assert.That(sql).Contains("WHERE");
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Beta");
        await Assert.That(results[1].Name).IsEqualTo("Gamma");
        await Assert.That(results[2].Name).IsEqualTo("Alpha");
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Beta");   // 2024-06-15
        await Assert.That(results[1].Name).IsEqualTo("Gamma");  // 2024-03-10
        await Assert.That(results[2].Name).IsEqualTo("Alpha");  // 2024-01-01
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
        await Assert.That(results).IsEmpty();
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WithSelectProjection_ShowsOrderByBeforeSelect()
    {
        await using var context = NewModelOnlyContext();

        // Verify the SQL shows ORDER BY is applied correctly
        var query = context.TestEntities
            .Where(_ => _.Name != "")
            .Select(_ => new TestEntity { Id = _.Id, Name = _.Name });

        var sql = query.ToQueryString();

        // SQL should contain ORDER BY and it should work correctly
        await Assert.That(sql).Contains("ORDER BY");
        await Assert.That(sql).Contains("CreatedDate");
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

        await Assert.That(results).Count().IsEqualTo(3);
        // All should have Ids, ordered by CreatedDate descending
        await Assert.That(results.All(_ => _.Id > 0)).IsTrue();
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
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].Name).IsEqualTo("Beta");   // 2024-06-15
        await Assert.That(results[1].Name).IsEqualTo("Alpha");  // 2024-01-01
        await Verify(results);
    }

    [Test]
    public async Task ToQueryString_WithWhereNullComparisonAndSelectProjection()
    {
        await using var context = NewModelOnlyContext();

        // This exact scenario was failing in GraphQL.EntityFramework
        var query = context.TestEntities
            .Where(_ => string.Equals(_.Name, null))
            .Select(_ => new TestEntity { Id = _.Id });

        // Should not throw translation error
        var sql = query.ToQueryString();

        await Assert.That(sql).Contains("WHERE");
        await Assert.That(sql).Contains("ORDER BY");
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Engineering");  // DisplayOrder 1
        await Assert.That(results[1].Name).IsEqualTo("Sales");        // DisplayOrder 2
        await Assert.That(results[2].Name).IsEqualTo("HR");           // DisplayOrder 3

        // Verify nested collections have employees
        await Assert.That(results[0].Employees).Count().IsEqualTo(3);
        await Assert.That(results[1].Employees).Count().IsEqualTo(2);
        await Assert.That(results[2].Employees).Count().IsEqualTo(1);

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
        await Assert.That(sql).Contains("DisplayOrder");
        await Assert.That(sql).Contains("DESC");

        // Results ordered by DisplayOrder descending
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].DisplayOrder).IsEqualTo(3);  // HR
        await Assert.That(results[1].DisplayOrder).IsEqualTo(2);  // Sales
        await Assert.That(results[2].DisplayOrder).IsEqualTo(1);  // Engineering

        // Employee collections properly populated
        await Assert.That(results[0].Employees).Count().IsEqualTo(1); // HR
        await Assert.That(results[1].Employees).Count().IsEqualTo(2); // Sales
        await Assert.That(results[2].Employees).Count().IsEqualTo(3); // Engineering

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
        await Assert.That(results[0].DisplayOrder).IsEqualTo(1);  // Engineering
        await Assert.That(results[1].DisplayOrder).IsEqualTo(2);  // Sales
        await Assert.That(results[2].DisplayOrder).IsEqualTo(3);  // HR

        // Child ordering is applied within each parent: HireDate descending
        var engEmployees = results[0].Employees;
        await Assert.That(engEmployees[0].HireDate).IsEqualTo(new DateTime(2024, 3, 20));  // Bob (newest)
        await Assert.That(engEmployees[1].HireDate).IsEqualTo(new DateTime(2024, 1, 15));  // Alice
        await Assert.That(engEmployees[2].HireDate).IsEqualTo(new DateTime(2023, 6, 10));  // Charlie (oldest)

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
        await Assert.That(results[0].DisplayOrder).IsEqualTo(1);  // Engineering
        await Assert.That(results[1].DisplayOrder).IsEqualTo(2);  // Sales
        await Assert.That(results[2].DisplayOrder).IsEqualTo(3);  // HR

        // Verify the SQL contains both DisplayOrder and Id in ORDER BY
        await Assert.That(sql).Contains("ORDER BY");
        await Assert.That(sql).Contains("DisplayOrder");
        await Assert.That(sql).Contains("Id");

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
    public async Task DerivedType_InheritsBaseOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.DerivedEntitiesA.ToListAsync();

        // Should be ordered by SortOrder ascending (inherited from BaseEntity)
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("DerivedA2"); // SortOrder 1
        await Assert.That(results[1].Name).IsEqualTo("DerivedA1"); // SortOrder 2
        await Assert.That(results[2].Name).IsEqualTo("DerivedA3"); // SortOrder 3
        await Verify(results);
    }

    [Test]
    public async Task BaseType_StillWorksWithOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.BaseEntities.ToListAsync();

        // Should be ordered by SortOrder ascending (all types in TPH table)
        await Assert.That(results[0].SortOrder).IsLessThanOrEqualTo(results[1].SortOrder);
        await Verify(results);
    }

    [Test]
    public async Task DerivedType_WithExplicitQueryOrderBy_OverridesInherited()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.DerivedEntitiesA
            .OrderByDescending(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name descending (explicit query), not SortOrder (inherited default)
        await Assert.That(results[0].Name).IsEqualTo("DerivedA3");
        await Assert.That(results[1].Name).IsEqualTo("DerivedA2");
        await Assert.That(results[2].Name).IsEqualTo("DerivedA1");
        await Verify(results);
    }

    [Test]
    public async Task DerivedType_WithConfiguredOrderBy_OverridesBaseOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.DerivedEntitiesB.ToListAsync();

        // DerivedEntityB has its own .OrderByDescending(_ => _.Name) configured in OnModelCreating
        // This should take precedence over BaseEntity's .OrderBy(_ => _.SortOrder)
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].Name).IsEqualTo("DerivedB2"); // Name DESC: B2 > B1
        await Assert.That(results[1].Name).IsEqualTo("DerivedB1");
        await Verify(results);
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
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Engineering");  // DisplayOrder 1
        await Assert.That(results[1].Name).IsEqualTo("Sales");        // DisplayOrder 2
        await Assert.That(results[2].Name).IsEqualTo("HR");           // DisplayOrder 3

        // Verify the nested employees are ordered by Id (from the Select projection)
        await Assert.That(results[0].Employees[0].Id).IsLessThan(results[0].Employees[1].Id);

        await Verify(results);
    }

    static TestDbContext NewModelOnlyContext()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=.;Database=Test;");
        builder.UseDefaultOrderBy();
        return new(builder.Options);
    }
}