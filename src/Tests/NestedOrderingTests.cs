// Ordering that appears inside a lambda belongs to a subquery. It must not be mistaken
// for explicit ordering of the query the lambda is nested in.
[TestFixture]
public class NestedOrderingTests
{
    [Test]
    public async Task OrderingInsideWhereLambda_StillAppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Employees
            .Where(_ => _.Tasks.OrderBy(task => task.Title).Count() >= 0)
            .ToListAsync();

        // Employees keep their default ordering of HireDate descending
        Assert.That(results.Select(_ => _.Name), Is.EqualTo(new[]
        {
            "Frank",   // 2024-04-10
            "Bob",     // 2024-03-20
            "Diana",   // 2024-02-05
            "Alice",   // 2024-01-15
            "Eve",     // 2023-11-01
            "Charlie"  // 2023-06-10
        }));
        await Verify(results);
    }

    [Test]
    public async Task OrderingInsideAnyLambda_StillAppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Employees
            .Where(_ => !_.Tasks.OrderBy(task => task.Title).Any(task => task.Priority < 0))
            .ToListAsync();

        Assert.That(results[0].Name, Is.EqualTo("Frank"));
        Assert.That(results[^1].Name, Is.EqualTo("Charlie"));
        await Verify(results);
    }

    [Test]
    public async Task OrderingInsideSelectManyLambda_StillAppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .SelectMany(_ => _.Employees.OrderBy(employee => employee.Name))
            .ToListAsync();

        // The projected employees get their own default ordering of HireDate descending
        Assert.That(results[0].Name, Is.EqualTo("Frank"));
        Assert.That(results[^1].Name, Is.EqualTo("Charlie"));
        await Verify(results);
    }

    [Test]
    public async Task OrderingInsideIncludeFilter_StillOrdersNestedCollection()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees.Where(employee => employee.Tasks.OrderBy(task => task.Title).Count() >= 0))
            .ToListAsync();

        // The ordering inside the filter applies to Tasks, so Employees still get their default
        var engineering = results[0].Employees;
        Assert.That(engineering.Select(_ => _.Name), Is.EqualTo(new[]
        {
            "Bob",     // 2024-03-20
            "Alice",   // 2024-01-15
            "Charlie"  // 2023-06-10
        }));
        await Verify(results);
    }
}
