// Ordering that reaches into a JSON mapped column. EF Core translates the property path to a
// read of the JSON document, so the default ordering applies the same way it does to a column.
[TestFixture]
public class JsonColumnTests
{
    static readonly SqlInstance<JsonDbContext> sqlInstance = new(
        constructInstance: builder =>
        {
            builder.UseDefaultOrderBy();
            return new(builder.Options);
        },
        buildTemplate: async context =>
        {
            await context.Database.EnsureCreatedAsync();

            context.Products.AddRange(
                new()
                {
                    Name = "Widget",
                    Metadata = new()
                    {
                        Rank = 3
                    },
                    Tags =
                    [
                        new() { Value = "gamma" },
                        new() { Value = "alpha" },
                        new() { Value = "beta" }
                    ]
                },
                new()
                {
                    Name = "Gadget",
                    Metadata = new()
                    {
                        Rank = 1
                    }
                },
                new()
                {
                    Name = "Doohickey",
                    Metadata = new()
                    {
                        Rank = 2
                    }
                });

            context.Articles.AddRange(
                new()
                {
                    Title = "Oldest",
                    Info = new()
                    {
                        Audit = new()
                        {
                            Modified = new(2023, 1, 1)
                        }
                    }
                },
                new()
                {
                    Title = "Newest",
                    Info = new()
                    {
                        Audit = new()
                        {
                            Modified = new(2025, 1, 1)
                        }
                    }
                },
                new()
                {
                    Title = "Middle",
                    Info = new()
                    {
                        Audit = new()
                        {
                            Modified = new(2024, 1, 1)
                        }
                    }
                });

            context.Items.AddRange(
                new()
                {
                    Name = "B-light",
                    Category = "B",
                    Details = new()
                    {
                        Weight = 1
                    }
                },
                new()
                {
                    Name = "A-heavy",
                    Category = "A",
                    Details = new()
                    {
                        Weight = 9
                    }
                },
                new()
                {
                    Name = "A-light",
                    Category = "A",
                    Details = new()
                    {
                        Weight = 2
                    }
                });

            await context.SaveChangesAsync();
        });

    [Test]
    public async Task JsonProperty_AppliesDefaultOrder()
    {
        await using var database = await sqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Products.ToListAsync();

        Assert.That(results.Select(_ => _.Name), Is.EqualTo(["Gadget", "Doohickey", "Widget"]));
        await Verify(results);
    }

    [Test]
    public async Task NestedJsonProperty_AppliesDefaultOrder()
    {
        await using var database = await sqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Articles.ToListAsync();

        // Info.Audit.Modified descending, two owned types deep
        Assert.That(results.Select(_ => _.Title), Is.EqualTo(["Newest", "Middle", "Oldest"]));
        await Verify(results);
    }

    [Test]
    public async Task MixedColumnAndJsonProperties_AppliesDefaultOrder()
    {
        await using var database = await sqlInstance.Build();
        await using var context = database.NewDbContext();

        Recording.Start();
        var results = await context.Items.ToListAsync();

        // Category ascending, then Details.Weight descending
        Assert.That(results.Select(_ => _.Name), Is.EqualTo(["A-heavy", "A-light", "B-light"]));
        await Verify(results);
    }

    [Test]
    public async Task ExplicitOrdering_SuppressesJsonDefault()
    {
        await using var database = await sqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.Products
            .OrderBy(_ => _.Name)
            .ToListAsync();

        Assert.That(results.Select(_ => _.Name), Is.EqualTo(["Doohickey", "Gadget", "Widget"]));
    }

    [Test]
    public async Task JsonProperty_AppliesBeneathTake()
    {
        await using var database = await sqlInstance.Build();
        await using var context = database.NewDbContext();

        // The default ordering has to be applied before Take, otherwise an arbitrary row is taken
        var results = await context.Products
            .Take(1)
            .ToListAsync();

        Assert.That(results.Single().Name, Is.EqualTo("Gadget"));
    }

    // A JSON mapped collection is read out of its parent's JSON document. EF Core throws on an
    // ordered Include over one in a tracking query, so it has to be left in document order.
    [Test]
    public async Task JsonCollection_KeepsDocumentOrder()
    {
        await using var database = await sqlInstance.Build();
        await using var context = database.NewDbContext();

        var results = await context.Products
            .Include(_ => _.Tags)
            .ToListAsync();

        var widget = results.Single(_ => _.Name == "Widget");
        Assert.That(widget.Tags.Select(_ => _.Value), Is.EqualTo(["gamma", "alpha", "beta"]));
    }

    [Test]
    public void JsonPath_SkipsIndexCreation()
    {
        using var context = NewModelOnlyContext();

        // A JSON property is not a column of the entity's table, so there is nothing to index
        var product = context.Model.FindEntityType(typeof(JsonProduct))!;
        Assert.That(product.GetIndexes(), Is.Empty);

        var article = context.Model.FindEntityType(typeof(JsonArticle))!;
        Assert.That(article.GetIndexes(), Is.Empty);

        // A composite ordering is skipped whole when any one of its clauses reaches into JSON
        var item = context.Model.FindEntityType(typeof(JsonItem))!;
        Assert.That(item.GetIndexes(), Is.Empty);
    }

    // Validation runs on the first query rather than when the model is built
    [Test]
    public void JsonOwnedTypes_DoNotRequireOrdering()
    {
        var builder = new DbContextOptionsBuilder<JsonRequiredContext>()
            .UseSqlServer("Server=.;Database=Test;");
        builder.UseDefaultOrderBy(requireOrderingForAllEntities: true);

        using var context = new JsonRequiredContext(builder.Options);

        // The owned types behind a JSON column are not separately queryable,
        // so ordering must not be demanded for them
        Assert.DoesNotThrow(() => context.Entities.ToQueryString());
    }

    [Test]
    public void RedundantJsonOrderBy_Throws()
    {
        using var context = new JsonRedundantContext(redundantOptions);

        var exception = Assert.Throws<Exception>(
            () => context.Entities
                .OrderBy(_ => _.Meta.Rank)
                .ToQueryString())!;

        Assert.That(exception.Message, Does.Contain("JsonRedundantEntity"));
        Assert.That(exception.Message, Does.Contain("OrderBy(Meta.Rank)"));
    }

    [Test]
    public void DifferentJsonOrderBy_DoesNotThrow()
    {
        using var context = new JsonRedundantContext(redundantOptions);

        // A different property of the same JSON column is not the configured ordering
        Assert.DoesNotThrow(
            () => context.Entities
                .OrderBy(_ => _.Meta.Label)
                .ToQueryString());
    }

    static readonly DbContextOptions<JsonRedundantContext> redundantOptions = BuildRedundantOptions();

    static DbContextOptions<JsonRedundantContext> BuildRedundantOptions()
    {
        var builder = new DbContextOptionsBuilder<JsonRedundantContext>()
            .UseSqlServer("Server=.;Database=Test;");
        builder.UseDefaultOrderBy(throwOnRedundantOrderBy: true);
        return builder.Options;
    }

    static JsonDbContext NewModelOnlyContext()
    {
        var builder = new DbContextOptionsBuilder<JsonDbContext>()
            .UseSqlServer("Server=.;Database=Test;");
        builder.UseDefaultOrderBy();
        return new(builder.Options);
    }
}

public class JsonProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public JsonProductMetadata Metadata { get; set; } = new();
    public List<JsonProductTag> Tags { get; set; } = [];
}

public class JsonProductMetadata
{
    public int Rank { get; set; }
    public string Label { get; set; } = "";
}

public class JsonProductTag
{
    public string Value { get; set; } = "";
}

public class JsonArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public JsonArticleInfo Info { get; set; } = new();
}

public class JsonArticleInfo
{
    public JsonArticleAudit Audit { get; set; } = new();
}

public class JsonArticleAudit
{
    public DateTime Modified { get; set; }
}

public class JsonItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public JsonItemDetails Details { get; set; } = new();
}

public class JsonItemDetails
{
    public int Weight { get; set; }
}

public class JsonRedundantEntity
{
    public int Id { get; set; }
    public JsonRedundantMeta Meta { get; set; } = new();
}

public class JsonRedundantMeta
{
    public int Rank { get; set; }
    public string Label { get; set; } = "";
}

public class JsonRequiredEntity
{
    public int Id { get; set; }
    public JsonRequiredMeta Meta { get; set; } = new();
}

public class JsonRequiredMeta
{
    public int Rank { get; set; }
}

class JsonRedundantContext(DbContextOptions<JsonRedundantContext> options) :
    DbContext(options)
{
    public DbSet<JsonRedundantEntity> Entities => Set<JsonRedundantEntity>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        var entity = model.Entity<JsonRedundantEntity>();
        entity.OwnsOne(_ => _.Meta, _ => _.ToJson());
        entity.OrderBy(_ => _.Meta.Rank);
    }
}

class JsonRequiredContext(DbContextOptions<JsonRequiredContext> options) :
    DbContext(options)
{
    public DbSet<JsonRequiredEntity> Entities => Set<JsonRequiredEntity>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        var entity = model.Entity<JsonRequiredEntity>();
        entity.OwnsOne(_ => _.Meta, _ => _.ToJson());
        entity.OrderBy(_ => _.Meta.Rank);
    }
}

public class JsonDbContext(DbContextOptions<JsonDbContext> options) :
    DbContext(options)
{
    public DbSet<JsonProduct> Products => Set<JsonProduct>();
    public DbSet<JsonArticle> Articles => Set<JsonArticle>();
    public DbSet<JsonItem> Items => Set<JsonItem>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        var product = model.Entity<JsonProduct>();
        product.OwnsOne(_ => _.Metadata, _ => _.ToJson());
        product.OwnsMany(_ => _.Tags, _ => _.ToJson());
        product.OrderBy(_ => _.Metadata.Rank);

        var article = model.Entity<JsonArticle>();
        article.OwnsOne(
            _ => _.Info,
            owned =>
            {
                owned.ToJson();
                owned.OwnsOne(_ => _.Audit);
            });
        article.OrderByDescending(_ => _.Info.Audit.Modified);

        var item = model.Entity<JsonItem>();
        item.OwnsOne(_ => _.Details, _ => _.ToJson());
        item.Property(_ => _.Category).HasMaxLength(450);
        item.OrderBy(_ => _.Category)
            .ThenByDescending(_ => _.Details.Weight);
    }
}
