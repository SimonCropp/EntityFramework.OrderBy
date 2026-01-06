public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}

public class AnotherEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Priority { get; set; }
}

public class EntityWithoutDefaultOrder
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

public class EntityWithMultipleOrderings
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public int Priority { get; set; }
    public string Name { get; set; } = "";
}
