// The default ordering has to be applied before any operator that chooses rows, otherwise
// an arbitrary subset is chosen first and only then sorted.
public class RowLimitingTests
{
    // Employees in default order of HireDate descending:
    // Frank, Bob, Diana, Alice, Eve, Charlie

    [Test]
    public async Task Take_OrdersBeforeLimiting()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Employees
            .Take(3)
            .ToListAsync();

        await Assert.That(results.Select(_ => _.Name)).IsEquivalentTo(["Frank", "Bob", "Diana"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task SkipTake_OrdersBeforeLimiting()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Employees
            .Skip(1)
            .Take(2)
            .ToListAsync();

        await Assert.That(results.Select(_ => _.Name)).IsEquivalentTo(["Bob", "Diana"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task IncludeThenPage_OrdersBeforeLimiting()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Departments
            .Include(_ => _.Employees)
            .Skip(1)
            .Take(1)
            .ToListAsync();

        // Departments in default order are Engineering, Sales, HR
        await Assert.That(results.Single().Name).IsEqualTo("Sales");

        // The included collection keeps its own default ordering
        await Assert.That(results[0].Employees.Select(_ => _.Name)).IsEquivalentTo(["Diana", "Eve"], CollectionOrdering.Matching);
        await Verify(results);
    }

    [Test]
    public async Task First_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var result = await context.Employees.FirstAsync();

        await Assert.That(result.Name).IsEqualTo("Frank");
        await Verify(result);
    }

    [Test]
    public async Task FirstOrDefaultWithPredicate_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var result = await context.Employees.FirstOrDefaultAsync(_ => _.Salary > 70000);

        // Of Alice, Bob, Charlie and Eve, Bob was hired most recently
        await Assert.That(result!.Name).IsEqualTo("Bob");
        await Verify(result);
    }

    [Test]
    public async Task SelectThenFirst_AppliesDefaultOrderToSource()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var result = await context.Employees
            .Select(_ => _.Name)
            .FirstAsync();

        await Assert.That(result).IsEqualTo("Frank");
        await Verify(result);
    }

    [Test]
    public async Task ElementAt_AppliesDefaultOrder()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var result = await context.Employees.ElementAtAsync(2);

        await Assert.That(result.Name).IsEqualTo("Diana");
        await Verify(result);
    }

    [Test]
    public async Task Count_DoesNotApplyOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var result = await context.Employees.CountAsync();

        // Ordering an aggregate is pointless work, so it must be left alone
        await Assert.That(result).IsEqualTo(6);
        await Verify(result);
    }

    [Test]
    public async Task Single_DoesNotApplyOrdering()
    {
        await using var database = await ModuleInitializer.SqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var result = await context.Employees.SingleAsync(_ => _.Name == "Eve");

        // Single matches at most one row, so ordering it is pointless work
        await Assert.That(result.Salary).IsEqualTo(72000);
        await Verify(result);
    }
}
