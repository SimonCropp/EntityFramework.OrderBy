# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build
dotnet build src --configuration Release

# Run tests
dotnet test src --configuration Release --no-build --no-restore

# Run a single test
dotnet test src --configuration Release --filter "FullyQualifiedName~TestMethodName"
```

## Project Overview

EntityFramework.OrderBy is a NuGet package that applies default ordering to Entity Framework Core queries via fluent configuration. It intercepts EF Core queries and automatically applies configured ordering to queries without explicit `OrderBy` clauses.

**Package**: [EfOrderBy on NuGet](https://www.nuget.org/packages/EfOrderBy/)

## Architecture

The library uses EF Core's query interceptor pipeline:

1. **Configuration** (`OnModelCreating`): Developer configures ordering via `.OrderBy()` fluent API. Configuration stored in entity type annotations.

2. **Interception** (`Interceptor.cs`): Implements `IQueryExpressionInterceptor`. On `QueryCompilationStarting`:
   - `IncludeOrderingApplicator` visits `Include()` expressions and orders nested collections
   - `OrderingDetector` checks if explicit ordering exists
   - Applies default ordering only when no explicit `OrderBy` exists

3. **Expression Building**: `OrderByClause` pre-builds expression trees and generic method calls for performance. Uses both `Queryable` (for DbSet) and `Enumerable` (for nested collections) methods.

### Key Components

| File | Purpose |
|------|---------|
| `OrderByExtensions.cs` | Entry point - `UseDefaultOrderBy()` and `OrderBy()` extensions |
| `OrderByBuilder.cs` | Fluent API for `ThenBy()`/`ThenByDescending()` chaining |
| `Interceptor.cs` | Query expression interceptor - main processing logic |
| `IncludeOrderingApplicator.cs` | ExpressionVisitor for `Include()` ordering |
| `OrderingDetector.cs` | ExpressionVisitor to detect existing `OrderBy` |
| `OrderByClause.cs` | Pre-built expression trees for ordering operations |
| `Configuration.cs` | Stores entity ordering configuration |
| `RequiredOrder.cs` | Validation for required ordering mode |

## Development Notes

- **Solution**: `src/EntityFramework.OrderBy.slnx`
- **Target**: .NET 10.0, C# latest
- **Strict mode**: Warnings as errors, code style enforcement enabled
- **Testing**: NUnit with Verify (snapshot testing)
- **Documentation**: Code snippets in `Snippets.cs` auto-sync to `readme.md` via MarkdownSnippets

### Performance Considerations

The codebase heavily uses caching and pre-built expressions:
- `ConcurrentDictionary` caches configuration per entity type
- `OrderByClause` pre-computes generic method calls and parameter expressions
- Validation runs once per `DbContext` type
