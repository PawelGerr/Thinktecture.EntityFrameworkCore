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

This repository extends Entity Framework Core with performance enhancements and testing utilities across multiple database providers. The architecture follows a layered approach with shared abstractions and provider-specific implementations.

### Core Package Structure

**Runtime Packages (`src/`)**:

1. **`Thinktecture.EntityFrameworkCore.Relational`** - Foundation layer
   - Base abstractions for all relational providers
   - Window functions (`RowNumber`, `PartitionBy`, `OrderBy`)
   - LEFT JOIN support via `LeftJoin()` extension methods
   - Table hints abstraction (`ITableHint`, `WithTableHints()`)
   - Nested transaction support (`NestedRelationalTransactionManager`)
   - Entity data readers (`IEntityDataReader`, `EntityDataReader`)
   - Default schema handling (`IDbDefaultSchema`, `DefaultSchemaModelCustomizer`)
   - Tenant database provider infrastructure (`ITenantDatabaseProvider`)
   - Query translation extensions and SQL expression types

2. **`Thinktecture.EntityFrameworkCore.BulkOperations`** - Bulk operations layer
   - Provider-agnostic interfaces:
     - `IBulkInsertExecutor` - Bulk insert operations
     - `IBulkUpdateExecutor` - Bulk update operations
     - `IBulkInsertOrUpdateExecutor` - Bulk upsert (MERGE) operations
     - `ITruncateTableExecutor` - Table truncation
   - Temp table abstractions:
     - `ITempTableCreator` - Creates temp tables with lifecycle management
     - `ITempTableReference` - Represents a temp table instance
     - `ITempTableQuery<T>` - Queryable wrapper with automatic cleanup
     - `ITempTableBulkInsertExecutor` - Bulk insert into temp tables
   - Property selection strategies:
     - `IEntityPropertiesProvider` - Base interface for property filtering
     - `IncludingEntityPropertiesProvider` - Include specific properties
     - `ExcludingEntityPropertiesProvider` - Exclude specific properties
   - Temp table name management with suffix leasing for concurrency
   - Collection parameter support (`ICollectionParameterFactory`, `ScalarCollectionParameter`)

3. **`Thinktecture.EntityFrameworkCore.SqlServer`** - SQL Server implementation
   - Bulk operations via `SqlBulkCopy` (inserts) and MERGE statements (updates/upserts)
   - SQL Server-specific options:
     - `SqlServerBulkInsertOptions` - Bulk insert configuration
     - `SqlServerBulkUpdateOptions` - Bulk update configuration
     - `SqlServerBulkInsertOrUpdateOptions` - Upsert configuration
     - `SqlServerTempTableBulkInsertOptions` - Temp table bulk insert options
   - Table hints: `SqlServerTableHint` enum with values like `NoLock`, `ReadPast`, `UpdLock`, etc.
   - Temp table support with SQL Server-specific features
   - JSON collection parameters for passing complex data structures
   - Window function implementations
   - Migration customizations (`ThinktectureSqlServerMigrationsSqlGenerator`)
   - Context factories for managing connections and transactions
   - Query translation and SQL generation customizations

4. **`Thinktecture.EntityFrameworkCore.Sqlite`** - SQLite implementation
   - Simplified bulk operations using INSERT statements
   - SQLite-specific options:
     - `SqliteBulkInsertOptions` - Bulk insert configuration
     - `SqliteBulkUpdateOptions` - Bulk update configuration
     - `SqliteBulkInsertOrUpdateOptions` - Upsert configuration
     - `SqliteAutoIncrementBehavior` - Control auto-increment handling
   - Temp table support adapted for SQLite limitations
   - Command builders for generating SQLite-compatible SQL
   - Query translation customizations for SQLite dialect

5. **`Thinktecture.EntityFrameworkCore.Testing`** - Testing infrastructure base
   - Test context providers (`ITestDbContextProvider`, `TestDbContextProviderBuilder`)
   - Migration execution strategies:
     - `IMigrationExecutionStrategy` - Base interface
     - `MigrationExecutionStrategy` - Runs pending migrations
     - `EnsureCreatedMigrationExecutionStrategy` - Uses EnsureCreated
     - `NoMigrationExecutionStrategy` - No migration execution
   - Command capturing (`CommandCapturingInterceptor`) for SQL verification
   - Per-context model cache (`CachePerContextModelCacheKeyFactory`)
   - Logging infrastructure (`SubLoggerFactory`, `TestingLoggingOptions`)
   - Async enumerable helpers for testing

6. **`Thinktecture.EntityFrameworkCore.SqlServer.Testing`** - SQL Server test helpers
   - `SqlServerDbContextIntegrationTests` - Base class for SQL Server integration tests
   - `SqlServerTestDbContextProvider` - SQL Server-specific test context provider
   - Test isolation via lock tables (`SqlServerLockTableOptions`)
   - Shared connection management for test performance

7. **`Thinktecture.EntityFrameworkCore.Sqlite.Testing`** - SQLite test helpers
   - `SqliteDbContextIntegrationTests<T>` - Base class for SQLite integration tests
   - `SqliteTestDbContextProvider` - SQLite-specific test context provider
   - In-memory database support for fast tests
   - Context provider factory for test fixtures

**Test Structure (`tests/`)**:
- Tests mirror the `src/` structure with `.Tests` suffix
- `TestHelpers` package provides shared test utilities
- Integration tests use provider-specific base classes

### Key Architectural Patterns

**Bulk Operations Architecture**:
- **Provider-agnostic design**: Interfaces in `BulkOperations` package define contracts
- **Provider-specific implementations**: SQL Server uses `SqlBulkCopy` for fast inserts; SQLite uses batched INSERT statements
- **Context factories**: Manage connection lifecycle, transactions, and resource cleanup
- **Options pattern**: Strongly-typed options classes (`SqlServerBulkInsertOptions`, etc.) configure behavior
- **Property providers**: Control which entity properties participate in bulk operations
- **Owned entity support**: Special handling for EF Core owned entities with complex object graphs

**Temp Tables Architecture**:
- **Lifecycle management**: `ITempTableCreator` creates tables; `ITempTableReference` handles cleanup
- **Queryable integration**: `ITempTableQuery<T>` wraps `IQueryable<T>` with `IAsyncDisposable` for automatic cleanup
- **Bulk insert integration**: `ITempTableBulkInsertExecutor` enables fast population of temp tables
- **Name management**: Suffix-based naming with leasing mechanism prevents conflicts in concurrent scenarios
- **Primary key strategies**: Multiple providers (`ConfiguredPrimaryKeyPropertiesProvider`, `AdaptiveEntityTypeConfigurationPrimaryKeyPropertiesProvider`) control PK creation
- **Caching**: Statement caching (`TempTableStatementCache`) improves performance for repeated operations
- **Provider-specific features**: SQL Server supports persistent temp tables; SQLite uses temporary tables

**Window Functions Architecture**:
- **Fluent API**: `EF.Functions.RowNumber()`, `EF.Functions.PartitionBy()`, `EF.Functions.OrderBy()`
- **Server-side evaluation**: Translates to native SQL window functions
- **Provider support**: Implemented in both SQL Server and SQLite (via query translators)
- **Expression-based**: Uses custom SQL expressions (`WindowFunctionExpression`, `WindowFunctionPartitionByExpression`)

**LEFT JOIN Support**:
- **Extension methods**: Multiple `LeftJoin()` overloads on `IQueryable<T>`
- **Result type**: Returns `LeftJoinResult<TOuter, TInner>` with nullable inner entity
- **Query translation**: Custom expression visitors translate to SQL LEFT JOIN
- **Type safety**: Strongly-typed results with proper null handling

**Table Hints (SQL Server)**:
- **Type-safe API**: `WithTableHints()` extension accepts `SqlServerTableHint` enum
- **Query integration**: Hints applied via custom query translation
- **Common hints**: `NoLock`, `ReadPast`, `UpdLock`, `HoldLock`, `RowLock`, `PageLock`, `TabLock`
- **Limited variants**: `SqlServerTableHintLimited` for restricted contexts

**Nested Transactions**:
- **Manager**: `NestedRelationalTransactionManager` wraps EF Core transaction manager
- **Transaction types**: `RootNestedDbContextTransaction` (physical), `ChildNestedDbContextTransaction` (logical)
- **Savepoint simulation**: Child transactions use commit/rollback tracking without actual nested transactions
- **Lifecycle tracking**: Maintains transaction stack for proper cleanup

**Collection Parameters**:
- **Scalar collections**: `ScalarCollectionParameter<T>` for primitive value lists
- **Factory pattern**: `ICollectionParameterFactory` creates provider-specific implementations
- **SQL Server JSON**: `JsonCollectionParameter` uses JSON for complex collections
- **Query translation**: Integrates with EF Core query pipeline for parameterized queries

**Tenant Database Support**:
- **Provider interface**: `ITenantDatabaseProvider` abstracts tenant-specific database selection
- **Factory**: `ITenantDatabaseProviderFactory` creates providers per query context
- **Query integration**: Custom query context factory injects tenant information
- **Dummy implementation**: Default no-op provider for single-database scenarios

**Entity Data Readers**:
- **ADO.NET bridge**: `IEntityDataReader` exposes entities as `IDataReader`
- **Bulk operation support**: Enables `SqlBulkCopy` to read from entity collections
- **Property navigation**: `PropertyWithNavigations` handles complex property paths
- **Value extraction**: Supports regular properties, shadow properties, and navigation properties
- **Factory pattern**: `IEntityDataReaderFactory` creates readers from entity collections

**Multi-Provider Support**:
- **Layered abstractions**: Relational → BulkOperations → Provider-specific
- **Shared infrastructure**: Common code in base packages; providers implement specifics
- **SQL Server features**: Table hints, JSON parameters, MERGE statements, temp tables
- **SQLite adaptations**: Simplified bulk operations, command builders, dialect handling
- **Extension points**: Providers customize query translation, SQL generation, and migration handling

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

### Essential Patterns and API Usage

#### Bulk Operations
```csharp
// Bulk insert entities
await dbContext.BulkInsertAsync(entities, options =>
{
    options.PropertyProvider = // optional: control which properties to insert
});

// Bulk update entities
await dbContext.BulkUpdateAsync(entities, options =>
{
    options.PropertyProvider = // optional: control which properties to update
});

// Bulk insert or update (upsert/MERGE)
await dbContext.BulkInsertOrUpdateAsync(entities, options =>
{
    // SQL Server: Uses MERGE statement
    // SQLite: Uses INSERT ... ON CONFLICT
});

// Truncate table
await dbContext.TruncateTableAsync<MyEntity>();
```

#### Temp Tables
```csharp
// Create and populate temp table from entities
await using var tempTable = await dbContext.BulkInsertIntoTempTableAsync(entities);

// Create and populate temp table from scalar values
await using var tempTable = await dbContext.BulkInsertValuesIntoTempTableAsync(
    new[] { 1, 2, 3 },
    builder => builder.HasColumn<int>("Value")
);

// Use temp table in queries
var results = await tempTable.Query
    .Join(dbContext.Orders, t => t.Id, o => o.CustomerId, (t, o) => o)
    .ToListAsync();

// Temp table is automatically dropped when disposed
```

#### Window Functions
```csharp
// Row number with partition and order
var query = dbContext.Orders
    .Select(o => new
    {
        Order = o,
        RowNum = EF.Functions.RowNumber(
            EF.Functions.PartitionBy(o.CustomerId),
            EF.Functions.OrderBy(o.OrderDate)
        )
    });

// Multiple partitions and ordering
var rowNum = EF.Functions.RowNumber(
    EF.Functions.PartitionBy(o.Category, o.Region),
    EF.Functions.OrderBy(o.Date).ThenByDescending(o.Amount)
);
```

#### LEFT JOIN
```csharp
// LEFT JOIN with null-safe result
var query = dbContext.Customers
    .LeftJoin(
        dbContext.Orders,
        customer => customer.Id,
        order => order.CustomerId,
        (customer, order) => new { Customer = customer, Order = order.Value }
    )
    .Where(x => x.Order == null || x.Order.Amount > 100);
```

#### Table Hints (SQL Server)
```csharp
// Single hint
var query = dbContext.Orders
    .WithTableHints(SqlServerTableHint.NoLock);

// Multiple hints
var query = dbContext.Orders
    .WithTableHints(SqlServerTableHint.NoLock, SqlServerTableHint.ReadPast);
```

#### Nested Transactions
```csharp
// Outer transaction
await using var transaction1 = await dbContext.Database.BeginTransactionAsync();

// Nested transaction (logical savepoint)
await using var transaction2 = await dbContext.Database.BeginTransactionAsync();

await transaction2.CommitAsync(); // Commits nested transaction
await transaction1.CommitAsync(); // Commits outer transaction
```

#### Property Selection in Bulk Operations
```csharp
// Include specific properties
await dbContext.BulkInsertAsync(entities, options =>
{
    options.PropertyProvider = IncludingEntityPropertiesProvider.Include(
        e => e.Name,
        e => e.Email
    );
});

// Exclude specific properties
await dbContext.BulkUpdateAsync(entities, options =>
{
    options.PropertyProvider = ExcludingEntityPropertiesProvider.Exclude(
        e => e.CreatedAt,
        e => e.Id
    );
});
```

#### Collection Parameters
```csharp
// Create scalar collection parameter
var param = dbContext.CreateScalarCollectionParameter(new[] { 1, 2, 3 });

// Use in queries (provider translates to appropriate SQL)
var orders = await dbContext.Orders
    .Where(o => param.Contains(o.CustomerId))
    .ToListAsync();
```

#### General Async Patterns
```csharp
// Proper async method signature with cancellation
public async Task<Result> ProcessAsync(CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(parameter);
    var data = await GetDataAsync(cancellationToken);
    return ProcessData(data);
}

// Always prefer server-side evaluation
var query = dbContext.Orders
    .Where(o => o.Status == OrderStatus.Pending) // Server-side
    .AsNoTracking(); // Read-only optimization

// Avoid client evaluation
var results = await query.ToListAsync(); // Execute on server first
```

## Important Files to Know

- `Thinktecture.EntityFrameworkCore.slnx` - Main solution file
- `Directory.Packages.props` - Centralized package version management
- `Directory.Build.props` - Shared MSBuild properties
- `.github/copilot-instructions.md` - Comprehensive development guidelines
- Test results are written to `test-results/<TargetFramework>/`