public static class ModuleInit
{
    public static SqlInstance<TestDbContext> SqlInstance = null!;

    [ModuleInitializer]
    public static void Initialize()
    {
        SqlInstance = new(
            constructInstance: builder =>
            {
                builder.UseDefaultOrderBy();
                return new TestDbContext(builder.Options);
            },
            buildTemplate: async context =>
            {
                await context.Database.EnsureCreatedAsync();

                context.TestEntities.AddRange(
                    new TestEntity { Name = "Alpha", CreatedDate = new DateTime(2024, 1, 1) },
                    new TestEntity { Name = "Beta", CreatedDate = new DateTime(2024, 6, 15) },
                    new TestEntity { Name = "Gamma", CreatedDate = new DateTime(2024, 3, 10) }
                );

                context.AnotherEntities.AddRange(
                    new AnotherEntity { Name = "Zebra", Priority = 1 },
                    new AnotherEntity { Name = "Apple", Priority = 3 },
                    new AnotherEntity { Name = "Mango", Priority = 2 }
                );

                context.EntitiesWithoutDefaultOrder.AddRange(
                    new EntityWithoutDefaultOrder { Value = "Third" },
                    new EntityWithoutDefaultOrder { Value = "First" },
                    new EntityWithoutDefaultOrder { Value = "Second" }
                );

                // Test data for multiple orderings: Category ASC, Priority DESC, Name ASC
                context.EntitiesWithMultipleOrderings.AddRange(
                    new EntityWithMultipleOrderings { Category = "B", Priority = 1, Name = "Item1" },
                    new EntityWithMultipleOrderings { Category = "A", Priority = 2, Name = "Item2" },
                    new EntityWithMultipleOrderings { Category = "A", Priority = 2, Name = "Item1" },
                    new EntityWithMultipleOrderings { Category = "A", Priority = 1, Name = "Item3" },
                    new EntityWithMultipleOrderings { Category = "B", Priority = 2, Name = "Item4" }
                );

                await context.SaveChangesAsync();
            });

        VerifyEntityFramework.Initialize();
    }
}
