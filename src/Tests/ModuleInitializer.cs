public static class ModuleInitializer
{
    public static SqlInstance<TestDbContext> SqlInstance = null!;

    [ModuleInitializer]
    public static void Initialize()
    {
        SqlInstance = new(
            constructInstance: builder =>
            {
                builder.UseDefaultOrderBy();
                return new(builder.Options);
            },
            buildTemplate: async context =>
            {
                await context.Database.EnsureCreatedAsync();

                context.TestEntities.AddRange(
                    new TestEntity { Name = "Alpha", CreatedDate = new(2024, 1, 1) },
                    new TestEntity { Name = "Beta", CreatedDate = new(2024, 6, 15) },
                    new TestEntity { Name = "Gamma", CreatedDate = new(2024, 3, 10) }
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

                // Test data for Department-Employee relationship
                var dept1 = new Department
                {
                    Name = "Engineering",
                    DisplayOrder = 1,
                    Employees =
                    [
                        new() { Name = "Alice", HireDate = new(2024, 1, 15), Salary = 90000 },
                        new() { Name = "Bob", HireDate = new(2024, 3, 20), Salary = 85000 },
                        new() { Name = "Charlie", HireDate = new(2023, 6, 10), Salary = 95000 }
                    ]
                };

                var dept2 = new Department
                {
                    Name = "Sales",
                    DisplayOrder = 2,
                    Employees =
                    [
                        new() { Name = "Diana", HireDate = new(2024, 2, 5), Salary = 70000 },
                        new() { Name = "Eve", HireDate = new(2023, 11, 1), Salary = 72000 }
                    ]
                };

                var dept3 = new Department
                {
                    Name = "HR",
                    DisplayOrder = 3,
                    Employees =
                    [
                        new() { Name = "Frank", HireDate = new(2024, 4, 10), Salary = 65000 }
                    ]
                };

                context.Departments.AddRange(dept1, dept2, dept3);

                await context.SaveChangesAsync();
            });

        VerifyEntityFramework.Initialize();
    }
}
