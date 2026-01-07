[TestFixture]
public class DefaultOrderByTests
{
    [Test]
    public async Task QueryWithoutOrderBy_AppliesDefaultDescendingOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities.ToListAsync();

        // Should be ordered by CreatedDate descending (newest first)
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));  // 2024-03-10
        Assert.That(results[2].Name, Is.EqualTo("Alpha"));  // 2024-01-01
    }

    [Test]
    public async Task QueryWithoutOrderBy_AppliesDefaultAscendingOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.AnotherEntities.ToListAsync();

        // Should be ordered by Name ascending
        Assert.That(results[0].Name, Is.EqualTo("Apple"));
        Assert.That(results[1].Name, Is.EqualTo("Mango"));
        Assert.That(results[2].Name, Is.EqualTo("Zebra"));
    }

    [Test]
    public async Task QueryWithExplicitOrderBy_DoesNotApplyDefault()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .OrderBy(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Alpha"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Gamma"));
    }

    [Test]
    public async Task EntityWithoutConfiguration_NoDefaultOrderApplied()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Should work without throwing - no ordering guaranteed
        var results = await context.EntitiesWithoutDefaultOrder.ToListAsync();

        Assert.That(results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task QueryWithWhere_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .Where(_ => _.Name != "Alpha")
            .ToListAsync();

        // Should still apply default ordering
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));  // 2024-03-10
    }

    [Test]
    public async Task QueryWithMultipleOrderings_AppliesAllInOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

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
    }

    [Test]
    public async Task IncludeWithoutExplicitOrdering_AppliesDefaultOrderingToNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

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
    }

    [Test]
    public async Task IncludeWithExplicitOrdering_DoesNotApplyDefaultToNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

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
    }

    [Test]
    public async Task ParentQueryWithoutOrderBy_AppliesDefaultToParentOnly()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Query departments without Include - should apply default ordering
        var results = await context.Departments.ToListAsync();

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));  // DisplayOrder 1
        Assert.That(results[1].Name, Is.EqualTo("Sales"));        // DisplayOrder 2
        Assert.That(results[2].Name, Is.EqualTo("HR"));           // DisplayOrder 3
    }

    [Test]
    public async Task ParentQueryWithExplicitOrderBy_DoesNotApplyDefault()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

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
    }

    [Test]
    public async Task QueryWithOrderByDescending_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .OrderByDescending(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name descending (explicit), not CreatedDate descending (default)
        Assert.That(results[0].Name, Is.EqualTo("Gamma"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Alpha"));
    }

    [Test]
    public async Task QueryWithOrderByThenBy_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Add some test data with same CreatedDate to test ThenBy
        context.TestEntities.Add(new() { Name = "Delta", CreatedDate = DateTime.Parse("2024-06-15") });
        await context.SaveChangesAsync();

        var results = await context.TestEntities
            .OrderBy(_ => _.CreatedDate)
            .ThenBy(_ => _.Name)
            .ToListAsync();

        // Should use explicit ordering, not default
        var betaDelta = results.Where(_ => _.CreatedDate == DateTime.Parse("2024-06-15")).ToList();
        Assert.That(betaDelta[0].Name, Is.EqualTo("Beta"));
        Assert.That(betaDelta[1].Name, Is.EqualTo("Delta"));
    }

    [Test]
    public async Task QueryWithWhereAndExplicitOrderBy_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .Where(_ => _.Name != "Alpha")
            .OrderBy(_ => _.Name)
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Beta"));
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));
    }

    [Test]
    public async Task QueryWithSelectAndExplicitOrderBy_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .OrderBy(_ => _.Name)
            .Select(_ => new { _.Name, _.CreatedDate })
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Alpha"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Gamma"));
    }

    [Test]
    public async Task QueryWithThenByDescending_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

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
    }

    [Test]
    public async Task QueryWithMultipleExplicitOrderings_IgnoresDefaultOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.Departments
            .OrderBy(_ => _.Name)
            .ThenByDescending(_ => _.DisplayOrder)
            .ToListAsync();

        // Should use explicit ordering (Name ASC, DisplayOrder DESC), not default (DisplayOrder ASC)
        Assert.That(results[0].Name, Is.EqualTo("Engineering"));
        Assert.That(results[1].Name, Is.EqualTo("HR"));
        Assert.That(results[2].Name, Is.EqualTo("Sales"));
    }

    [Test]
    public async Task IncludeWithExplicitOrderByDescending_IgnoresDefaultForNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.Departments
            .Include(_ => _.Employees.OrderByDescending(_ => _.Name))
            .ToListAsync();

        // Employees should be ordered by Name descending (explicit), not HireDate descending (default)
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees[0].Name, Is.EqualTo("Charlie"));
        Assert.That(engEmployees[1].Name, Is.EqualTo("Bob"));
        Assert.That(engEmployees[2].Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task IncludeWithExplicitThenBy_IgnoresDefaultForNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.Departments
            .Include(_ => _.Employees.OrderBy(_ => _.Salary).ThenBy(_ => _.Name))
            .ToListAsync();

        // Employees should use explicit ordering (Salary ASC, Name ASC), not default (HireDate DESC)
        var engEmployees = results[0].Employees;
        Assert.That(engEmployees, Has.Count.EqualTo(3));

        // Verify they're ordered by Salary first, then Name
        Assert.That(engEmployees[0].Salary, Is.LessThanOrEqualTo(engEmployees[1].Salary));
        Assert.That(engEmployees[1].Salary, Is.LessThanOrEqualTo(engEmployees[2].Salary));
    }
}
