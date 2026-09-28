public class InheritedOrderingTests
{
    static DbContextOptions options =
        new DbContextOptionsBuilder<InternalOrderContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .UseDefaultOrderBy()
            .Options;

    [Test]
    public async Task NonPublicProperty_InheritsOrderingToDerivedType()
    {
        using var context = new InternalOrderContext(options);

        var sql = context.Derived.ToQueryString();

        await Assert.That(sql).Contains("ORDER BY");
        await Assert.That(sql).Contains("SortOrder");
    }
}

public class InternalOrderBase
{
    public int Id { get; set; }

    // Mapped explicitly below, since conventions only pick up public properties
    internal int SortOrder { get; set; }
}

public class InternalOrderDerived : InternalOrderBase
{
    public string Extra { get; set; } = "";
}

public class InternalOrderContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<InternalOrderBase> Bases => Set<InternalOrderBase>();
    public DbSet<InternalOrderDerived> Derived => Set<InternalOrderDerived>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<InternalOrderBase>()
            .Property(_ => _.SortOrder);

        builder.Entity<InternalOrderBase>()
            .OrderBy(_ => _.SortOrder);
    }
}
