# GitHub Copilot Instructions

These rules guide GitHub Copilot in this repo so generated code, tests, and docs fit our stack and conventions. This document is optimized to help Copilot provide better support for feature development, bug fixing, and testing in the Thinktecture.EntityFrameworkCore repository.

## Quick Start for Development Tasks

### For Feature Development
1. **Understand scope**: Identify affected packages in `src/**` and corresponding tests in `tests/**`
2. **Test-first approach**: Write failing tests before implementation
3. **Minimal public surface**: Keep APIs `internal` unless cross-package usage is needed
4. **Server-evaluable queries**: Ensure EF Core operations stay on the server side
5. **Async + CancellationToken**: Always provide async APIs for I/O operations

### For Bug Fixes
1. **Reproduce first**: Create a failing test demonstrating the bug
2. **Minimal fix**: Implement the smallest change to pass the test
3. **Regression protection**: Ensure adequate test coverage for the fix
4. **Multi-TFM validation**: Test on both `net8.0` and `net9.0`

### For Test Writing
1. **Mirror structure**: Tests follow `tests/<Package>.Tests/**` structure
2. **Naming convention**: `MethodName_Should_DoSomething_When_Condition`
3. **Integration patterns**: Use `IntegrationTestsBase` for SQL Server, appropriate base for SQLite
4. **Deterministic**: Avoid time-based operations, use predictable test data

## Repo Overview

- **Purpose**: Extensions for Entity Framework Core (performance features, SQL Server/SQLite helpers, test utilities)
- **Packages**: Relational, SqlServer, Sqlite, BulkOperations, and Testing variants
- **Structure**:
  - Runtime code: `src/**`
  - Tests: `tests/**`
  - Samples/benchmarks: `samples/**`

## Tech Stack and Targets

- **.NET**: net8.0 and net9.0
- **C#**: LangVersion 13.0 (nullable enabled, implicit usings enabled)
- **EF Core**: 9.x (Microsoft.EntityFrameworkCore.*: 9.0.8)
- **Testing**: xUnit + AwesomeAssertions + NSubstitute + Serilog XUnit sink
- **Logging**: Serilog with XUnit sink for tests/samples
- **Package management**: Central versions in `Directory.Packages.props`
- **Key dependencies**: Microsoft.Data.SqlClient 5.2.3, BenchmarkDotNet 0.15.2, Testcontainers.MsSql 4.6.0

## Core Conventions Copilot Must Follow

### Code Style and Structure
- **Namespaces**: Start with `Thinktecture` and match folder structure
- **Nullable reference types**: Enabled; use explicit null checks with `ArgumentNullException.ThrowIfNull(...)`
- **Async first**: Provide async APIs with `CancellationToken` for I/O operations; avoid sync-over-async
- **Visibility**: Keep public surface minimal; prefer `internal` unless used across packages
- **File organization**: Use file-scoped namespaces, expression-bodied members where appropriate
- **Immutability**: Use `readonly` fields/structs where intended; prefer `record` for value-like objects

### EF Core Specific Patterns
- **Query composition**: Keep operations server-evaluable; avoid client evaluation; prefer `IQueryable` over early `ToList()`
- **SQL safety**: Never concatenate raw SQL; use `FromSqlInterpolated` or existing parameterized helpers
- **Read-only queries**: Use `AsNoTracking()` unless state tracking is needed
- **Transactions**: Use EF Core `IDbContextTransaction`/`ExecutionStrategy` patterns
- **Provider specifics**:
  - **SQL Server**: Use/extend existing abstractions for table hints, temp tables, bulk operations
  - **SQLite**: Respect provider limitations; add conditional code paths for different behavior

## Key Architecture Patterns

### Bulk Operations Architecture
- **Executors**: Implement `IBulkInsertExecutor`, `IBulkUpdateExecutor`, `IBulkInsertOrUpdateExecutor`, `ITruncateTableExecutor`
- **SQL Server implementation**: `SqlServerBulkOperationExecutor` uses `SqlBulkCopy` for inserts and MERGE statements for updates
- **Context factories**: `ISqlServerBulkOperationContextFactory` creates contexts with readers and connection management
- **Options pattern**: Strongly-typed options inherit from base interfaces (e.g., `SqlServerBulkInsertOptions : IBulkInsertOptions`)
- **Property providers**: Use `IEntityPropertiesProvider` for flexible property selection in bulk operations

### Bulk Operations Usage Patterns
- **Insert operations**: `BulkInsertAsync<T>()` extensions on `DbContext` with strongly-typed options
- **Update operations**: `BulkUpdateAsync<T>()` with key properties and update properties specification
- **Upsert operations**: `BulkInsertOrUpdateAsync<T>()` for MERGE-style operations
- **Temp table integration**: `BulkInsertIntoTempTableAsync<T>()` returns `ITempTableQuery<T>` for further querying
- **Value insertion**: `BulkInsertValuesIntoTempTableAsync<T>()` for inserting simple values into temp tables

### Temp Tables Architecture
- **Creation**: `ITempTableCreator` creates temp tables with `ITempTableReference` for lifecycle management
- **Querying**: `ITempTableQuery<T>` wraps `IQueryable<T>` with automatic cleanup via `IAsyncDisposable`
- **Bulk insertion**: Integration with bulk operations via `ITempTableBulkInsertExecutor`
- **SQL generation**: Provider-specific SQL generation for temp table DDL and cleanup

### Query Extensions Patterns
- **Table hints**: `query.WithTableHints(SqlServerTableHint.NoLock)` for SQL Server query optimization
- **Tenant databases**: Automatic table name prefixing via `ITenantDatabaseProvider` injection
- **Window functions**: `EF.Functions.WindowFunction()` for analytical queries with partitioning and ordering

### Window Functions Architecture
- **Function definitions**: `WindowFunction<T>` represents SQL window functions with return type
- **Translation**: Custom query translation via `SqlServerDbFunctionsExtensions`
- **Usage pattern**: `EF.Functions.WindowFunction(function, arg, EF.Functions.PartitionBy(...), EF.Functions.OrderBy(...))`

### Database Provider Abstraction
- **Multi-provider support**: Shared interfaces in `BulkOperations` with provider-specific implementations
- **SQL Server specifics**: Table hints, temp tables, window functions, tenant database support
- **SQLite specifics**: Simplified bulk operations, window functions, limitations handling
- **Provider factories**: `SqlServerTestDbContextProviderFactory<T>`, `SqliteTestDbContextProviderFactory<T>`

## Testing Patterns and Architecture

### Test Infrastructure
- **Base classes**: Use `IntegrationTestsBase` for SQL Server tests, `SqliteDbContextIntegrationTests<T>` for SQLite tests
- **Test isolation**: Support for shared tables with ambient transactions vs. migration rollback strategies
- **Database providers**: `SqlServerTestDbContextProvider<T>` and `SqliteTestDbContextProvider<T>` manage test database lifecycle
- **Fixtures**: Use `SqlServerFixture` for shared SQL Server connections, `DbContextProviderFactoryFixture` for SQLite
- **Collections**: Mark SQL Server tests with `[Collection("SqlServerTests")]` for proper isolation
- **Command capturing**: Enable via `CollectExecutedCommands()` to verify generated SQL in tests

### Test Context Patterns

#### SQL Server Integration Test Structure
```csharp
[Collection("SqlServerTests")]
public class MyFeatureTests : IntegrationTestsBase
{
    public MyFeatureTests(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
        : base(testOutputHelper, sqlServerFixture)
    {
        // Configure model or options if needed
        ConfigureModel = builder => builder.ConfigureTempTable<Guid>();
        IsTenantDatabaseSupportEnabled = true; // for tenant features
    }

    [Fact]
    public async Task Should_perform_bulk_insert_when_entities_provided()
    {
        // Arrange: Use ArrangeDbContext for test data setup
        var entities = new[] 
        { 
            new TestEntity { Id = Guid.NewGuid(), RequiredName = "Test1" },
            new TestEntity { Id = Guid.NewGuid(), RequiredName = "Test2" }
        };

        // Act: Use ActDbContext for the operation being tested
        await ActDbContext.BulkInsertAsync(entities);

        // Assert: Use AssertDbContext for verification (optional, can use ActDbContext)
        var count = await AssertDbContext.TestEntities.CountAsync();
        count.Should().Be(2);
        
        // Verify SQL was executed
        ExecutedCommands.Should().ContainSingle(cmd => cmd.Contains("INSERT BULK"));
    }
}
```

#### SQLite Integration Test Structure
```csharp
public class MyFeatureTests : SqliteDbContextIntegrationTests<TestDbContext>
{
    public MyFeatureTests(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
        : base(testOutputHelper, providerFactoryFixture)
    {
    }

    [Fact]
    public async Task Should_create_temp_table_when_configured()
    {
        // Arrange & Act
        await using var tempTable = await ActDbContext.CreateTempTableAsync<TempTable<int>>();

        // Assert
        tempTable.Should().NotBeNull();
        tempTable.Query.Should().NotBeNull();
    }
}
```

#### Unit Test Patterns for Executors and Providers
```csharp
public class BulkOperationExecutorTests
{
    [Fact]
    public void CreateOptions_Should_return_proper_options_when_called()
    {
        // Arrange
        var executor = Substitute.For<IBulkInsertExecutor>();
        var propertiesProvider = Substitute.For<IEntityPropertiesProvider>();

        // Act
        var options = executor.CreateOptions(propertiesProvider);

        // Assert
        options.Should().NotBeNull();
        options.PropertiesToInsert.Should().Be(propertiesProvider);
    }
}
```

### Assertion Patterns
- **Fluent assertions**: Use AwesomeAssertions for readable assertions: `result.Should().HaveCount(3)`
- **Object comparisons**: Use `result.Should().BeEquivalentTo()` for complex object comparisons
- **Collection validation**: Use `result.Should().AllSatisfy(item => item.Property.Should().Be(expected))`
- **Query string validation**: `query.ToQueryString().Should().Be(expectedSql)`
- **Command execution verification**: `ExecutedCommands.Last().Should().Contain("EXPECTED SQL")`

### Mocking and Test Doubles
- **Framework**: Use NSubstitute for mocking: `Substitute.For<IInterface>()`
- **Mock setup**: `mock.Method(Arg.Any<Type>()).Returns(value)`
- **Tenant database mocking**: `TenantDatabaseProviderMock.GetDatabaseName(schema, table).Returns(database)`

### Specialized Testing Patterns
- **Bulk operations testing**: Create test entities with specific GUIDs for predictable assertions
- **Temp table testing**: `await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(values)`
- **Table hints testing**: `query.WithTableHints(SqlServerTableHint.NoLock).ToQueryString()`
- **Window functions**: Use predefined window function instances like `_averageInt = new("AVG")`

## Build, Test, and Local Development

### Building the Solution
```powershell
dotnet restore
dotnet build -c Release
```

### Running Tests
```powershell
dotnet test -c Release --no-build
```

### Key Points
- Test results are written to `test-results/<TargetFramework>` via VSTest settings
- Add dependencies by updating `Directory.Packages.props` (central versions) and referencing the package in the project's `.csproj`
- Use `CollectExecutedCommands()` to capture and verify SQL generation in tests

## Productive Workflows and Checklists for Copilot

Use these concise, repeatable flows when implementing features, fixing bugs, or writing tests in this repository. Prefer making small, targeted changes and validating with fast checks.

### Feature Development Workflow: Test-First, Minimal, Safe

#### Planning and Scope
1. **Identify affected packages**: Locate relevant packages in `src/**` and corresponding tests in `tests/**`
2. **Search existing abstractions**: Extend existing patterns instead of introducing new public types
3. **Determine provider specifics**: Consider SQL Server vs SQLite differences early

#### Design and Shape the Change
1. **Minimize public surface**: Default to `internal` visibility; require justification for `public` APIs
2. **Design async-first**: Add async APIs with optional `CancellationToken` for I/O/DB operations
3. **Ensure server evaluation**: Keep EF Core operations server-evaluable; prefer `IQueryable` composition
4. **Plan for both target frameworks**: Ensure compatibility with `net8.0` and `net9.0`

#### Test-First Implementation
1. **Write failing tests first**: Create unit tests in `tests/<Package>.Tests/**` folder
2. **Cover core scenarios**: Happy path + 1-2 edge cases; prefer `[Theory]` for data-driven tests
3. **Use appropriate test base**: `IntegrationTestsBase` for SQL Server, `SqliteDbContextIntegrationTests<T>` for SQLite
4. **Verify SQL generation**: Use `CollectExecutedCommands()` to validate generated SQL

#### Implementation
1. **Guard clause validation**: Validate inputs early with appropriate exceptions
2. **Respect nullability**: No `#nullable disable`; use explicit null checks
3. **Provider branching**: Add conditional paths only when necessary; reuse existing helpers
4. **Error handling**: Throw appropriate exceptions (`ArgumentException`, `InvalidOperationException`, etc.)

#### Validation and Documentation
1. **Build and test**: Ensure all tests pass on both `net8.0` and `net9.0`
2. **Check warnings**: No new analyzer violations or warnings
3. **Document public APIs**: Add XML docs with examples for public members
4. **Performance consideration**: Add/update benchmarks for performance-sensitive changes

#### Quick Feature Development Checklist
- [ ] Tests added/updated (unit first; integration when needed)
- [ ] Public API minimized; XML docs added where public
- [ ] Async + `CancellationToken` on I/O/DB methods
- [ ] Server-evaluable queries; no client evaluation
- [ ] Build + tests pass on `net8.0`/`net9.0`
- [ ] Provider differences handled appropriately
- [ ] Samples/docs/benchmarks updated if relevant

### Bug Fix Workflow: Reproduce, Fix, Guard

#### Reproduction Phase
1. **Write failing test**: Create a test that demonstrates the bug (unit preferred; integration if provider-specific)
2. **Isolate the issue**: Identify minimal reproduction case
3. **Verify on both TFMs**: Ensure bug manifests on both `net8.0` and `net9.0`

#### Fix Implementation
1. **Minimal change principle**: Implement smallest change to make test pass
2. **Avoid breaking changes**: Don't break existing public APIs
3. **Maintain provider parity**: Ensure fix works for both SQL Server and SQLite where applicable

#### Regression Protection
1. **Add regression coverage**: Ensure adequate test coverage beyond the failing test
2. **Validate full test suite**: Run complete test suite for both target frameworks
3. **Check for side effects**: Verify no unintended behavior changes

#### Bug Fix Checklist
- [ ] Failing test added that reproduces the issue
- [ ] Minimal fix implemented with clear guard clauses
- [ ] Regression coverage complete; no flaky patterns
- [ ] Build + tests green on both TFMs
- [ ] No breaking changes to public APIs

### Test Authoring Patterns and Guidelines

#### Test Structure and Organization
- **Framework stack**: xUnit + AwesomeAssertions + NSubstitute + Serilog XUnit sink
- **Naming convention**: `MethodName_Should_DoSomething_When_Condition`
- **File organization**: Mirror source folder structure in `tests/**`
- **Test isolation**: Prefer unit tests; use integration tests for database-specific behavior

#### SQL Server Integration Test Pattern
```csharp
[Collection("SqlServerTests")]
public class FeatureTests : IntegrationTestsBase
{
    public FeatureTests(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
        : base(testOutputHelper, sqlServerFixture)
    {
        ConfigureModel = builder => builder.ConfigureTempTable<Guid>();
        IsTenantDatabaseSupportEnabled = true; // if testing tenant features
    }

    [Fact]
    public async Task BulkInsert_Should_insert_entities_when_valid_data_provided()
    {
        // Arrange
        var entities = new[] { new TestEntity { Id = Guid.NewGuid(), Name = "Test" } };

        // Act
        await ActDbContext.BulkInsertAsync(entities);

        // Assert
        var count = await AssertDbContext.TestEntities.CountAsync();
        count.Should().Be(1);
        ExecutedCommands.Should().ContainSingle(cmd => cmd.Contains("INSERT BULK"));
    }
}
```

#### SQLite Integration Test Pattern
```csharp
public class FeatureTests : SqliteDbContextIntegrationTests<TestDbContext>
{
    public FeatureTests(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
        : base(testOutputHelper, providerFactoryFixture)
    {
    }

    [Fact]
    public async Task TempTable_Should_be_created_when_requested()
    {
        // Arrange & Act
        await using var tempTable = await ActDbContext.CreateTempTableAsync<TempTable<int>>();

        // Assert
        tempTable.Should().NotBeNull();
        tempTable.Query.Should().NotBeNull();
    }
}
```

#### Unit Test Pattern for Core Components
```csharp
public class ComponentTests
{
    [Fact]
    public void Method_Should_return_expected_result_when_valid_input()
    {
        // Arrange
        var mock = Substitute.For<IDependency>();
        mock.GetValue().Returns("expected");
        var sut = new SystemUnderTest(mock);

        // Act
        var result = sut.Process();

        // Assert
        result.Should().Be("expected");
        mock.Received(1).GetValue();
    }
}
```

#### Testing Best Practices
- **Deterministic tests**: Avoid time-based waits or random data; use predictable seeds
- **Assertion patterns**: Use AwesomeAssertions for fluent, readable assertions
- **Mock appropriately**: Use NSubstitute for interfaces; avoid over-mocking
- **SQL verification**: Use `ExecutedCommands` collection to verify generated SQL
- **Cleanup handling**: Use `using`/`await using` for disposable resources

### EF Core Development Guidelines

#### Query Safety and Performance
- **Server evaluation**: Ensure expressions are server-evaluable; avoid client-side method calls
- **SQL parameterization**: Never concatenate SQL strings; use `FromSqlInterpolated` or existing helpers
- **Query tracking**: Default to `AsNoTracking()` for read-only queries
- **Transaction patterns**: Use EF Core `IDbContextTransaction`/`ExecutionStrategy` patterns

#### Provider-Specific Considerations
- **SQL Server patterns**: Leverage table hints, temp tables, bulk operations via existing abstractions
- **SQLite limitations**: Account for concurrency, DDL locks, missing server features
- **Conditional behavior**: Add provider-specific code paths when necessary
- **Feature parity**: Maintain consistent API surface across providers where possible

#### Common Pitfalls to Avoid
- **Client evaluation**: Avoid LINQ methods that force client evaluation
- **Connection management**: Don't manually manage connections; use EF Core patterns
- **Sync-over-async**: Always propagate async/await and `CancellationToken`
- **Performance anti-patterns**: Avoid N+1 queries, excessive allocations in hot paths

### Performance and Optimization Guidelines

#### Performance-Sensitive Development
- **Hot path optimization**: Minimize allocations; prefer simple loops over LINQ in tight loops
- **Memory management**: Use spans/memory only when they provide material performance wins
- **Async propagation**: Avoid sync-over-async; always propagate `CancellationToken`
- **Benchmarking**: Add/update micro-benchmarks in `samples/Thinktecture.EntityFrameworkCore.Benchmarks`

#### Allocation Optimization
- **Struct usage**: Use `readonly struct` for small, immutable value types
- **Object pooling**: Consider object pooling for frequently allocated objects
- **String operations**: Use `StringBuilder` or string interpolation appropriately
- **Collection patterns**: Pre-size collections when possible; use appropriate collection types

### Dependency and Package Management

#### Central Package Management Rules
- **Version centralization**: All package versions in `Directory.Packages.props`
- **No inline versions**: Reference packages from `.csproj` without version numbers
- **Target framework support**: Ensure new packages support both `net8.0` and `net9.0`
- **Analyzer compliance**: Respect repository analyzers; don't disable warnings broadly

#### Adding New Dependencies
1. **Justify necessity**: Ensure dependency adds significant value
2. **Check compatibility**: Verify compatibility with both target frameworks
3. **Update central file**: Add version to `Directory.Packages.props`
4. **Reference appropriately**: Add `<PackageReference>` without version in project file

### Documentation and API Design

#### Public API Guidelines
- **XML documentation**: Required for all public APIs (`GenerateDocumentationFile` is enabled)
- **Namespace alignment**: Keep namespaces aligned with folder structure starting with `Thinktecture`
- **Code examples**: Include small examples in XML docs for commonly used APIs
- **Inheritance docs**: Use `<inheritdoc />` when implementing/overriding documented members

#### API Evolution Patterns
- **Backward compatibility**: Don't break public APIs without strong justification
- **Deprecation process**: Use `[Obsolete]` with clear migration path
- **Additive changes**: Prefer additive extensions over modifying existing behavior
- **Provider extensions**: Add provider-specific features as extensions where possible

### Quality Gates and Validation

#### Pre-Submission Checklist
- [ ] **Build success**: Solution builds for all target frameworks
- [ ] **Test passing**: All unit and integration tests pass
- [ ] **No warnings**: No new analyzer warnings or violations
- [ ] **Style compliance**: Code matches repository conventions
- [ ] **Documentation**: XML docs updated for public API changes
- [ ] **Performance**: Benchmarks updated for performance-sensitive changes

#### Code Review Preparation
- [ ] **Clear scope**: PR has focused, clear motivation and scope
- [ ] **Test coverage**: Changes include appropriate test coverage
- [ ] **Provider behavior**: SQL Server vs SQLite differences documented
- [ ] **Migration notes**: Backward compatibility and migration considerations noted
- [ ] **Sample updates**: User-facing features include sample updates

## Essential Patterns Copilot Should Default To

### Code Organization Patterns
- **File-scoped namespaces**: Use file-scoped namespace declarations
- **Expression-bodied members**: Use where it improves clarity
- **Readonly enforcement**: Use `readonly` fields/structs where immutability is intended
- **Record types**: Prefer `record` for immutable value-like objects

### Guard Clause Patterns
```csharp
// Standard argument validation
ArgumentNullException.ThrowIfNull(parameter);
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

// Custom validation with clear messages
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
```

### Async/Await Patterns
```csharp
// Proper async method signature
public async Task<Result> ProcessAsync(CancellationToken cancellationToken = default)
{
    // Always propagate cancellation token
    var data = await GetDataAsync(cancellationToken);
    return ProcessData(data);
}
```

### Error Handling Patterns
```csharp
// Appropriate exception types
throw new InvalidOperationException("Operation not valid in current state.");
throw new ArgumentException("Invalid argument value.", nameof(argument));
throw new NotSupportedException("Feature not supported on this provider.");
```

## Repository Metadata

- **Solution entry**: `Thinktecture.EntityFrameworkCore.slnx`
- **CI pipeline**: `azure-pipelines.yml` (scripts under `ci/ci.ps1`)
- **Target frameworks**: `net8.0`, `net9.0`
- **EF Core version**: `9.x`
- **Test output**: `test-results/<TargetFramework>`
- **Structure**: runtime `src/**`, tests `tests/**`, samples/benchmarks `samples/**`
- **Package management**: `Directory.Packages.props`
- **Last updated**: 2025-08-10
