[TestFixture]
public class DefaultOrderByTests
{
    [Test]
    public async Task QueryWithoutOrderBy_AppliesDefaultDescendingOrder()
    {
        await using var database = await ModuleInit.SqlInstance.Build();
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
        await using var database = await ModuleInit.SqlInstance.Build();
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
        await using var database = await ModuleInit.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .OrderBy(e => e.Name)
            .ToListAsync();

        // Should be ordered by Name (explicit), not CreatedDate (default)
        Assert.That(results[0].Name, Is.EqualTo("Alpha"));
        Assert.That(results[1].Name, Is.EqualTo("Beta"));
        Assert.That(results[2].Name, Is.EqualTo("Gamma"));
    }

    [Test]
    public async Task EntityWithoutConfiguration_NoDefaultOrderApplied()
    {
        await using var database = await ModuleInit.SqlInstance.Build();
        await using var context = database.NewDbContext();

        // Should work without throwing - no ordering guaranteed
        var results = await context.EntitiesWithoutDefaultOrder.ToListAsync();

        Assert.That(results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task QueryWithWhere_AppliesDefaultOrder()
    {
        await using var database = await ModuleInit.SqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.TestEntities
            .Where(e => e.Name != "Alpha")
            .ToListAsync();

        // Should still apply default ordering
        Assert.That(results[0].Name, Is.EqualTo("Beta"));   // 2024-06-15
        Assert.That(results[1].Name, Is.EqualTo("Gamma"));  // 2024-03-10
    }

    [Test]
    public async Task QueryWithMultipleOrderings_AppliesAllInOrder()
    {
        await using var database = await ModuleInit.SqlInstance.Build();
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
}
