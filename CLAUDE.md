# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Development Commands

### Building the Solution
```powershell
dotnet restore
dotnet build -c Release
```

### Running Tests
```powershell
dotnet test -c Release --no-build
```

### Running a Single Test Project
```powershell
dotnet test tests/Thinktecture.EntityFrameworkCore.SqlServer.Tests/ -c Release
```

## Architecture Overview

This repository extends Entity Framework Core with performance enhancements and testing utilities across multiple database providers.

### Core Package Structure

**Runtime Packages (`src/`)**:
- `Thinktecture.EntityFrameworkCore.Relational` - Base abstractions and shared functionality
- `Thinktecture.EntityFrameworkCore.BulkOperations` - Multi-provider bulk operations
- `Thinktecture.EntityFrameworkCore.SqlServer` - SQL Server specific features
- `Thinktecture.EntityFrameworkCore.Sqlite` - SQLite specific features  
- `Thinktecture.EntityFrameworkCore.Testing` - Base testing infrastructure
- `Thinktecture.EntityFrameworkCore.SqlServer.Testing` - SQL Server test helpers
- `Thinktecture.EntityFrameworkCore.Sqlite.Testing` - SQLite test helpers

**Test Structure (`tests/`)**:
- Tests mirror the `src/` structure with `.Tests` suffix
- `TestHelpers` package provides shared test utilities

### Key Architectural Patterns

**Bulk Operations Architecture**:
- Provider-agnostic interfaces (`IBulkInsertExecutor`, `IBulkUpdateExecutor`, etc.)
- SQL Server implementation uses `SqlBulkCopy` for inserts and MERGE statements for updates
- Context factories manage connection lifecycle and transaction handling
- Strongly-typed options classes inherit from base interfaces

**Temp Tables Architecture**:
- `ITempTableCreator` creates temp tables with `ITempTableReference` for lifecycle management
- `ITempTableQuery<T>` wraps `IQueryable<T>` with automatic cleanup via `IAsyncDisposable`
- Integration with bulk operations via `ITempTableBulkInsertExecutor`

**Multi-Provider Support**:
- Shared interfaces in BulkOperations with provider-specific implementations
- SQL Server: Table hints, temp tables, window functions, tenant database support
- SQLite: Simplified bulk operations, window functions, limitation handling

## Testing Infrastructure

### SQL Server Integration Tests
- Base class: `IntegrationTestsBase`
- Mark tests with `[Collection("SqlServerTests")]` for proper isolation
- Use `SqlServerFixture` for shared connections
- Capture SQL with `CollectExecutedCommands()` to verify generated queries

### SQLite Integration Tests  
- Base class: `SqliteDbContextIntegrationTests<T>`
- Use `DbContextProviderFactoryFixture` for database lifecycle management
- Less isolation requirements compared to SQL Server tests

### Test Patterns
- xUnit + AwesomeAssertions + NSubstitute + Serilog XUnit sink
- Naming: `MethodName_Should_DoSomething_When_Condition`
- Use `ArrangeDbContext`, `ActDbContext`, `AssertDbContext` for different test phases
- Results written to `test-results/<TargetFramework>/`

## Development Workflows

### Feature Development (Test-First Approach)
1. **Understand scope**: Identify affected packages in `src/**` and corresponding tests in `tests/**`
2. **Write failing tests first**: Create unit tests in `tests/<Package>.Tests/**` folder
3. **Minimal public surface**: Keep APIs `internal` unless cross-package usage is needed
4. **Server-evaluable queries**: Ensure EF Core operations stay on the server side
5. **Use appropriate test base**: `IntegrationTestsBase` for SQL Server, `SqliteDbContextIntegrationTests<T>` for SQLite
6. **Verify SQL generation**: Use `CollectExecutedCommands()` to validate generated SQL

### Bug Fix Workflow
1. **Reproduce first**: Create a failing test demonstrating the bug
2. **Minimal fix**: Implement the smallest change to pass the test
3. **Multi-TFM validation**: Test on both `net8.0` and `net9.0`

## Key Development Guidelines

### EF Core Specifics
- Keep operations server-evaluable; avoid client evaluation; prefer `IQueryable` over early `ToList()`
- Never concatenate raw SQL; use `FromSqlInterpolated` or existing parameterized helpers
- Use `AsNoTracking()` for read-only queries unless state tracking is needed
- Always provide async APIs with `CancellationToken` for I/O operations; avoid sync-over-async
- Use EF Core `IDbContextTransaction`/`ExecutionStrategy` patterns for transactions

### Code Conventions
- Target frameworks: `net8.0` and `net9.0`
- C# 13.0 with nullable reference types enabled, implicit usings enabled
- File-scoped namespaces starting with `Thinktecture` and match folder structure
- Default to `internal` visibility unless cross-package usage is needed
- Use explicit null checks with `ArgumentNullException.ThrowIfNull()`
- Expression-bodied members where appropriate; prefer `record` for value-like objects

### Package Management
- All package versions centralized in `Directory.Packages.props`
- Reference packages from `.csproj` files without version numbers
- EF Core 9.x is the current target version (Microsoft.EntityFrameworkCore.*: 9.0.8)

### Provider-Specific Considerations
- SQL Server: Leverage table hints, temp tables, bulk operations via existing abstractions
- SQLite: Account for concurrency limitations, DDL locks, missing server features
- Add conditional code paths only when necessary; maintain consistent API surface where possible

### Essential Patterns
```csharp
// Proper async method signature
public async Task<Result> ProcessAsync(CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(parameter);
    var data = await GetDataAsync(cancellationToken);
    return ProcessData(data);
}

// Bulk operations usage
await ActDbContext.BulkInsertAsync(entities);
await using var tempTable = await ActDbContext.BulkInsertIntoTempTableAsync(values);

// Table hints (SQL Server)
query.WithTableHints(SqlServerTableHint.NoLock)

// Window functions
EF.Functions.WindowFunction(function, arg, EF.Functions.PartitionBy(...), EF.Functions.OrderBy(...))
```

## Important Files to Know

- `Thinktecture.EntityFrameworkCore.slnx` - Main solution file
- `Directory.Packages.props` - Centralized package version management
- `Directory.Build.props` - Shared MSBuild properties
- `.github/copilot-instructions.md` - Comprehensive development guidelines
- Test results are written to `test-results/<TargetFramework>/`