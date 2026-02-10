using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

[TestFixture]
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
    public void ProducesCreateIndexOperations()
    {
        var indexOps = GetDefaultOrderIndexOperations();
        Assert.That(indexOps, Has.Count.EqualTo(8));
        var indexNames = indexOps.Select(_ => _.Name).ToList();
        Assert.That(indexNames, Does.Contain("IX_TestEntity_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_AnotherEntity_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_EntityWithMultipleOrderings_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_Department_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_Employee_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_EmployeeTask_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_BaseEntity_DefaultOrder"));
        Assert.That(indexNames, Does.Contain("IX_DerivedEntityB_DefaultOrder"));
    }

    [Test]
    public void MultiColumnIndex_HasCorrectColumns()
    {
        var indexOps = GetDefaultOrderIndexOperations();
        var multiColumnOp = indexOps.Single(_ => _.Name == "IX_EntityWithMultipleOrderings_DefaultOrder");
        Assert.That(multiColumnOp.Columns, Is.EqualTo(["Category", "Priority", "Name"]));
    }

    [TestCase("IX_TestEntity_DefaultOrder", "CreatedDate")]
    [TestCase("IX_AnotherEntity_DefaultOrder", "Name")]
    [TestCase("IX_Department_DefaultOrder", "DisplayOrder")]
    [TestCase("IX_Employee_DefaultOrder", "HireDate")]
    [TestCase("IX_EmployeeTask_DefaultOrder", "Priority")]
    [TestCase("IX_BaseEntity_DefaultOrder", "SortOrder")]
    [TestCase("IX_DerivedEntityB_DefaultOrder", "Name")]
    public void SingleColumnIndex_HasCorrectColumn(string indexName, string expectedColumn)
    {
        var indexOps = GetDefaultOrderIndexOperations();
        var op = indexOps.Single(_ => _.Name == indexName);
        Assert.That(op.Columns, Is.EqualTo([expectedColumn]));
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
    public void DesignTimeModelHasNoConfigurationAnnotations()
    {
        using var context = new TestDbContext(CreateOptions());
        _ = context.Model;

        var designTimeModel = context.GetService<IDesignTimeModel>().Model;

        // Configuration annotations must be removed during model finalization.
        // If they leak into the design-time model, migration scaffolding crashes with:
        // "Cannot scaffold C# literals of type 'Configuration'"
        foreach (var entityType in designTimeModel.GetEntityTypes())
        {
            var annotation = entityType.FindAnnotation("DefaultOrderBy:Configuration");
            Assert.That(annotation, Is.Null,
                $"Entity {entityType.ClrType.Name} still has DefaultOrderBy:Configuration annotation");
        }

        // Model-level annotations should also be removed
        Assert.That(designTimeModel.FindAnnotation("DefaultOrderBy:InterceptorRegistered"), Is.Null);
        Assert.That(designTimeModel.FindAnnotation("DefaultOrderBy:IndexCreationDisabled"), Is.Null);
    }
}
