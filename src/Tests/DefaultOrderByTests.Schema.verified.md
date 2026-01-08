## Tables

### AnotherEntities

```sql
CREATE TABLE [dbo].[AnotherEntities](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Priority] [int] NOT NULL,
 CONSTRAINT [PK_AnotherEntities] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [IX_AnotherEntity_DefaultOrder] ON [dbo].[AnotherEntities]
(
	[Name] ASC
) ON [PRIMARY]
```

### Departments

```sql
CREATE TABLE [dbo].[Departments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
 CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Department_DefaultOrder] ON [dbo].[Departments]
(
	[DisplayOrder] ASC
) ON [PRIMARY]
```

### Employees

```sql
CREATE TABLE [dbo].[Employees](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DepartmentId] [int] NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[HireDate] [datetime2](7) NOT NULL,
	[Salary] [int] NOT NULL,
 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Employee_DefaultOrder] ON [dbo].[Employees]
(
	[HireDate] ASC
) ON [PRIMARY]
CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees]
(
	[DepartmentId] ASC
) ON [PRIMARY]
```

### EntitiesWithMultipleOrderings

```sql
CREATE TABLE [dbo].[EntitiesWithMultipleOrderings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Category] [nvarchar](450) NOT NULL,
	[Priority] [int] NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
 CONSTRAINT [PK_EntitiesWithMultipleOrderings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING ON

CREATE NONCLUSTERED INDEX [IX_EntityWithMultipleOrderings_DefaultOrder] ON [dbo].[EntitiesWithMultipleOrderings]
(
	[Category] ASC,
	[Priority] ASC,
	[Name] ASC
) ON [PRIMARY]
```

### EntitiesWithoutDefaultOrder

```sql
CREATE TABLE [dbo].[EntitiesWithoutDefaultOrder](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_EntitiesWithoutDefaultOrder] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
```

### TestEntities

```sql
CREATE TABLE [dbo].[TestEntities](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_TestEntities] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_TestEntity_DefaultOrder] ON [dbo].[TestEntities]
(
	[CreatedDate] ASC
) ON [PRIMARY]
```