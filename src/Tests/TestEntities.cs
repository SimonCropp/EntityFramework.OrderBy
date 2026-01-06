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

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }
    public List<Employee> Employees { get; set; } = new();
}

public class Employee
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = "";
    public DateTime HireDate { get; set; }
    public int Salary { get; set; }
}
