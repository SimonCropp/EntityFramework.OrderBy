// A collection navigation read inside a Select projection is loaded by a subquery of its own, and
// gets the default ordering of its entity the same way a collection in an Include does.
// Department: DisplayOrder. Employee: HireDate descending. EmployeeTask: Priority.
public class ProjectionOrderingTests
{
    [Test]
    public async Task ProjectedCollection_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();

        // The shape GraphQL.EntityFramework builds for a navigation list field
        var results = await context.Departments
            .Select(_ => new Department
            {
                Id = _.Id,
                Name = _.Name,
                Employees = _.Employees
                    .Select(employee => new Employee
                    {
                        Id = employee.Id,
                        Name = employee.Name,
                        HireDate = employee.HireDate
                    })
                    .ToList()
            })
            .ToListAsync();

        await Assert.That(results[0].Employees.Select(_ => _.Name)).IsEquivalentTo([
            "Bob",     // 2024-03-20
            "Alice",   // 2024-01-15
            "Charlie"  // 2023-06-10
        ], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task FilteredCollection_OrdersAfterFilter()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Select(_ => new
            {
                _.Name,
                Employees = _.Employees
                    .Where(employee => employee.Name != "Alice")
                    .Select(employee => employee.Name)
                    .ToList()
            })
            .ToListAsync();

        await Assert.That(results[0].Employees).IsEquivalentTo(["Bob", "Charlie"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task BoundNavigation_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Select(_ => new
            {
                _.Name,
                _.Employees
            })
            .ToListAsync();

        await Assert.That(results[0].Employees.Select(_ => _.Name)).IsEquivalentTo([
            "Bob",
            "Alice",
            "Charlie"
        ], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task NestedCollections_EachGetTheirDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .AsSingleQuery()
            .Select(_ => new
            {
                _.Name,
                Employees = _.Employees
                    .Select(employee => new
                    {
                        employee.Name,
                        Tasks = employee.Tasks
                            .Select(task => task.Title)
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        var engineering = results[0].Employees;
        await Assert.That(engineering.Select(_ => _.Name)).IsEquivalentTo(["Bob", "Alice", "Charlie"], CollectionOrdering.Matching);
        var alice = engineering[1];
        await Assert.That(alice.Tasks).IsEquivalentTo([
            "Code review", // Priority 1
            "Testing",     // Priority 2
            "Design"       // Priority 3
        ], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task FirstOrDefaultInProjection_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Select(_ => new
            {
                _.Name,
                NewestHire = _.Employees
                    .Select(employee => employee.Name)
                    .FirstOrDefault()
            })
            .ToListAsync();

        await Assert.That(results.Select(_ => _.NewestHire!)).IsEquivalentTo(["Bob", "Diana", "Frank"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task TakeInProjection_OrdersBeforeTake()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Select(_ => new
            {
                _.Name,
                Employees = _.Employees
                    .Take(2)
                    .Select(employee => employee.Name)
                    .ToList()
            })
            .ToListAsync();

        await Assert.That(results[0].Employees).IsEquivalentTo(["Bob", "Alice"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task ExplicitOrderingInProjection_IsKept()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Select(_ => new
            {
                _.Name,
                Employees = _.Employees
                    .OrderBy(employee => employee.Name)
                    .Select(employee => employee.Name)
                    .ToList()
            })
            .ToListAsync();

        await Assert.That(results[0].Employees).IsEquivalentTo(["Alice", "Bob", "Charlie"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task AggregateInProjection_IsNotOrdered()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var sql = context.Departments
            .Select(_ => new
            {
                _.Name,
                Count = _.Employees.Count(),
                AnyRecent = _.Employees.Any(employee => employee.HireDate > new DateTime(2024, 1, 1)),
                Payroll = _.Employees.Select(employee => employee.Salary).Sum()
            })
            .ToQueryString();

        await Assert.That(sql).DoesNotContain("ORDER BY [e");
        await Verify(sql);
    }

    [Test]
    public async Task DistinctInProjection_IsNotOrdered()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // EF Core drops ordering beneath a Distinct, so there is nothing to place it beneath
        var sql = context.Departments
            .Select(_ => new
            {
                _.Name,
                Names = _.Employees
                    .Select(employee => employee.Name)
                    .Distinct()
                    .ToList()
            })
            .ToQueryString();

        await Assert.That(sql).DoesNotContain("HireDate");
        await Verify(sql);
    }

    [Test]
    public async Task CollectionThatIsNotANavigation_IsLeftAlone()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Only the navigation is ordered. The list of names is a parameter, not a navigation.
        var names = new List<string> { "Bob", "Alice" };
        var sql = context.Departments
            .Select(_ => new
            {
                _.Name,
                Matching = _.Employees
                    .Where(employee => names.Contains(employee.Name))
                    .Select(employee => employee.Name)
                    .ToList()
            })
            .ToQueryString();

        await Assert.That(sql).Contains("[HireDate] DESC");
        await Verify(sql);
    }
}
