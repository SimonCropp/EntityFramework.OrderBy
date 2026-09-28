public class MigrationTests
{
    static DbContextOptions<TestDbContext> CreateOptions()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDefaultOrderBy();
        builder.UseSqlServer("Server=.;Database=Test;Trusted_Connection=True");
        return builder.Options;
    }

    static List<CreateIndexOperation> GetDefaultOrderIndexOperations()
    {
        using var context = new TestDbContext(CreateOptions());
        _ = context.Model;

        var differ = context.GetService<IMigrationsModelDiffer>();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var operations = differ.GetDifferences(null, designTimeModel.GetRelationalModel());

        return operations
            .OfType<CreateIndexOperation>()
            .Where(_ => _.Name.Contains("DefaultOrder"))
            .OrderBy(_ => _.Name)
            .ToList();
    }

    [Test]
    public async Task ProducesCreateIndexOperations()
    {
        var indexOps = GetDefaultOrderIndexOperations();
        await Assert.That(indexOps).Count().IsEqualTo(8);
        var indexNames = indexOps.Select(_ => _.Name).ToList();
        await Assert.That(indexNames).Contains("IX_TestEntity_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_AnotherEntity_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_EntityWithMultipleOrderings_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_Department_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_Employee_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_EmployeeTask_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_BaseEntity_DefaultOrder");
        await Assert.That(indexNames).Contains("IX_DerivedEntityB_DefaultOrder");
    }

    [Test]
    public async Task MultiColumnIndex_HasCorrectColumns()
    {
        var indexOps = GetDefaultOrderIndexOperations();
        var multiColumnOp = indexOps.Single(_ => _.Name == "IX_EntityWithMultipleOrderings_DefaultOrder");
        await Assert.That(multiColumnOp.Columns).IsEquivalentTo(["Category", "Priority", "Name"], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments("IX_TestEntity_DefaultOrder", "CreatedDate")]
    [Arguments("IX_AnotherEntity_DefaultOrder", "Name")]
    [Arguments("IX_Department_DefaultOrder", "DisplayOrder")]
    [Arguments("IX_Employee_DefaultOrder", "HireDate")]
    [Arguments("IX_EmployeeTask_DefaultOrder", "Priority")]
    [Arguments("IX_BaseEntity_DefaultOrder", "SortOrder")]
    [Arguments("IX_DerivedEntityB_DefaultOrder", "Name")]
    public async Task SingleColumnIndex_HasCorrectColumn(string indexName, string expectedColumn)
    {
        var indexOps = GetDefaultOrderIndexOperations();
        var op = indexOps.Single(_ => _.Name == indexName);
        await Assert.That(op.Columns).IsEquivalentTo([expectedColumn], CollectionOrdering.Matching);
    }

    [Test]
    public async Task DefaultOrderOperations()
    {
        var indexOps = GetDefaultOrderIndexOperations();
        var snapshot = indexOps.Select(_ => new
        {
            _.Name,
            _.Table,
            _.Columns,
            _.IsUnique,
            _.IsDescending
        });
        await Verify(snapshot);
    }

    [Test]
    public async Task DesignTimeModelHasNoConfigurationAnnotations()
    {
        using var context = new TestDbContext(CreateOptions());
        _ = context.Model;

        var designTimeModel = context.GetService<IDesignTimeModel>().Model;

        // Configuration annotations must be removed during model finalization.
        // If they leak into the design-time model, migration scaffolding crashes with:
        // "Cannot scaffold C# literals of type 'Configuration'"
        foreach (var entityType in designTimeModel.GetEntityTypes())
        {
            await Assert.That(entityType.GetOrderByConfiguration()).IsNull().Because($"Entity {entityType.ClrType.Name} still has DefaultOrderBy:Configuration annotation");
        }

        // Model-level annotations should also be removed
        await Assert.That(designTimeModel.IsInterceptorRegistered()).IsFalse();
        await Assert.That(designTimeModel.IsIndexCreationDisabled()).IsFalse();
    }

    [Test]
    public async Task ConflictingOrderingAcrossContexts_Throws()
    {
        // First context configures SharedEntity with OrderBy(Name)
        var options1 = new DbContextOptionsBuilder<ContextWithNameOrdering>()
            .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True")
            .UseDefaultOrderBy()
            .Options;

        using (var context = new ContextWithNameOrdering(options1))
        {
            _ = context.Model;
        }

        // Second context configures SharedEntity with OrderByDescending(Value) - should throw
        var options2 = new DbContextOptionsBuilder<ContextWithValueOrdering>()
            .UseSqlServer("Server=.;Database=Test;Trusted_Connection=True")
            .UseDefaultOrderBy()
            .Options;

        var exception = Assert.ThrowsExactly<Exception>(() =>
        {
            using var context = new ContextWithValueOrdering(options2);
            _ = context.Model;
        });

        await Assert.That(exception!.Message).Contains("SharedEntity");
        await Assert.That(exception.Message).Contains("Conflicting");
    }
}

public class SharedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

class ContextWithNameOrdering(DbContextOptions options) : DbContext(options)
{
    public DbSet<SharedEntity> Entities => Set<SharedEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<SharedEntity>()
            .OrderBy(_ => _.Name);
}

class ContextWithValueOrdering(DbContextOptions options) : DbContext(options)
{
    public DbSet<SharedEntity> Entities => Set<SharedEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<SharedEntity>()
            .OrderByDescending(_ => _.Value);
}
