# CLAUDE-TESTING.md

Testing instructions for the Thinktecture.EntityFrameworkCore repository.

## Test Framework Stack

- **xUnit** 2.9.3 - Test framework
- **AwesomeAssertions** 9.3.0 - Fluent assertion library (`.Should().Be(...)` style)
- **NSubstitute** 5.3.0 - Mocking framework (`Substitute.For<T>()`)
- **Serilog.Sinks.XUnit** - Routes log output to xUnit's test output
- **Testcontainers.MsSql** 4.10.0 - Docker-based SQL Server for integration tests

All test dependencies are declared in `tests/Directory.Build.props` with global usings:
```csharp
// Auto-imported in all test projects:
using AwesomeAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
```

## Test Project Structure

| Test Project | Tests For | Database |
|-------------|-----------|----------|
| `Relational.Tests` | Relational package (transactions, schema, migrations) | SQLite (in-memory) |
| `BulkOperations.Tests` | Bulk operations abstractions (no database needed) | SQLite (in-memory) |
| `SqlServer.Tests` | SQL Server-specific features | SQL Server (Testcontainers or configured) |
| `Sqlite.Tests` | SQLite-specific features | SQLite (in-memory) |
| `Testing.Tests` | Testing infrastructure itself | Varies |
| `TestHelpers` | Shared test entities, DbContext, utilities (not a test project) | N/A |

## Test Naming Convention

**Pattern:** `Should_DoSomething` or `Should_DoSomething_When_Condition`

Test classes are named after the feature/method being tested. Test methods always start with `Should_`.

```csharp
public class WithTableHints : IntegrationTestsBase
{
    [Fact]
    public async Task Should_add_table_hints_to_table() { ... }

    [Fact]
    public async Task Should_escape_index_name() { ... }

    [Fact]
    public async Task Should_add_table_hints_to_table_without_touching_included_navigations() { ... }
}
```

```csharp
public class Commit : NestedRelationalTransactionManagerTestBase
{
    [Fact]
    public void Should_throw_when_trying_to_commit_twice() { ... }

    [Fact]
    public void Should_commit_root_transaction_and_underlying_transaction() { ... }
}
```

## Arrange-Act-Assert (AAA) Pattern

All integration tests use **three separate DbContext instances** to ensure clean isolation between test phases:

```csharp
[Fact]
public async Task Should_insert_entities_via_bulk_insert()
{
    // ARRANGE - set up test data with ArrangeDbContext
    ArrangeDbContext.TestEntities.Add(new TestEntity { Id = guid, Name = "test", Count = 42 });
    ArrangeDbContext.SaveChanges();

    // ACT - execute the operation with ActDbContext
    var result = await ActDbContext.TestEntities.ToListAsync();

    // ASSERT - verify results (use AssertDbContext for DB reads if needed)
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("test");
}
```

The three contexts (`ArrangeDbContext`, `ActDbContext`, `AssertDbContext`) are provided by `TestCtxProvider` and share the same database but use independent change trackers.

## SQL Server Integration Tests

### Base Class & Fixtures

```
IntegrationTestsBase (in SqlServer.Tests project)
  └─ extends SqlServerDbContextIntegrationTests<TestDbContext>
      └─ uses [Collection("SqlServerTests")] for test isolation
          └─ via SqlServerTestsCollectionFixture : ICollectionFixture<SqlServerFixture>
```

**Every SQL Server test class must:**
1. Inherit from `IntegrationTestsBase`
2. Have the `[Collection("SqlServerTests")]` attribute (inherited from base)
3. Accept `ITestOutputHelper` and `SqlServerFixture` in its constructor

```csharp
public class MyFeatureTests : IntegrationTestsBase
{
    public MyFeatureTests(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
        : base(testOutputHelper, sqlServerFixture)
    {
    }

    [Fact]
    public async Task Should_do_something()
    {
        // Use ArrangeDbContext, ActDbContext, AssertDbContext
    }
}
```

### SQL Server Connection

`SqlServerFixture` supports two modes (controlled by config):
- **Testcontainers** (default in CI): Spins up a Docker SQL Server container (`UseSqlServerContainer=true`)
- **Configured connection string**: Uses `ConnectionStrings:default` from `appsettings.json` or User Secrets

Configuration in `tests/Thinktecture.EntityFrameworkCore.SqlServer.Tests/appsettings.json`:
```json
{
  "UseSqlServerContainer": true,
  "ConnectionStrings": {
    "default": "server=localhost;database=test;integrated security=true;TrustServerCertificate=true;Packet Size=32768;"
  }
}
```

### SQL Verification

Capture and inspect executed SQL commands:

```csharp
[Fact]
public async Task Should_generate_correct_sql()
{
    // ExecutedCommands is available because CollectExecutedCommands() is enabled in base
    await ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock).ToListAsync();

    ExecutedCommands.Last().Should().Contain("WITH (NOLOCK)");
}
```

You can also use `ToQueryString()` for pre-execution SQL inspection:
```csharp
var query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock);
query.ToQueryString().Should().Contain("WITH (NOLOCK)");
```

Command capturing is enabled in `IntegrationTestsBase.ConfigureTestDbContextProvider()` via `.CollectExecutedCommands()`.

### Custom Model Configuration

Override model building per-test by setting `ConfigureModel`:

```csharp
[Fact]
public async Task Should_handle_custom_entity_config()
{
    ConfigureModel = modelBuilder =>
    {
        modelBuilder.Entity<TestEntity>().Property(e => e.Name).HasMaxLength(50);
    };

    // Test code...
}
```

### Migration Strategy

SQL Server tests use `OneTimeMigrationStrategy` - migrations run once per test collection and are cached:
```csharp
public class OneTimeMigrationStrategy : IMigrationExecutionStrategy
{
    private bool _isMigrated;
    public void Migrate(DbContext ctx)
    {
        if (_isMigrated) return;
        ctx.Database.Migrate();
        _isMigrated = true;
    }
}
```

## SQLite Integration Tests

### Base Class & Fixtures

```
IntegrationTestsBase (in Sqlite.Tests project)
  └─ uses IAssemblyFixture<DbContextProviderFactoryFixture>
      └─ DbContextProviderFactoryFixture creates SqliteTestDbContextProvider per test
```

**Every SQLite test class must:**
1. Inherit from `IntegrationTestsBase`
2. Accept `ITestOutputHelper` and `DbContextProviderFactoryFixture` in its constructor

```csharp
public class MyFeatureTests : IntegrationTestsBase
{
    public MyFeatureTests(
        ITestOutputHelper testOutputHelper,
        DbContextProviderFactoryFixture providerFactoryFixture)
        : base(testOutputHelper, providerFactoryFixture)
    {
    }

    [Fact]
    public async Task Should_do_something()
    {
        // Use ArrangeDbContext, ActDbContext, AssertDbContext
    }
}
```

### Assembly Fixture Setup

SQLite uses xUnit's `AssemblyFixture` (from `Xunit.Extensions.AssemblyFixture` package) for assembly-wide initialization. The fixture is declared at the assembly level:

```csharp
[assembly: TestFramework(AssemblyFixtureFramework.TypeName, AssemblyFixtureFramework.AssemblyName)]
```

`DbContextProviderFactoryFixture` configures the SQLite test provider:
```csharp
public class DbContextProviderFactoryFixture : IAsyncLifetime
{
    private static SqliteTestDbContextProviderBuilder<TestDbContext> ConfigureBuilder()
    {
        return new SqliteTestDbContextProviderBuilder<TestDbContext>()
            .UseMigrationExecutionStrategy(IMigrationExecutionStrategy.Migrations)
            .UseMigrationLogLevel(LogLevel.Warning)
            .ConfigureSqliteOptions(optionsBuilder => optionsBuilder
                .AddBulkOperationSupport()
                .AddWindowFunctionsSupport());
    }
}
```

## Relational Tests (using SQLite)

The `Relational.Tests` project tests provider-agnostic features using SQLite as the database:

```csharp
public class IntegrationTestsBase : SqliteDbContextIntegrationTests<DbContextWithSchema>
{
    protected Action<ModelBuilder>? ConfigureModel { get; set; }
    // ...
}
```

## Test Entities and TestDbContext

Shared test models live in `tests/Thinktecture.EntityFrameworkCore.TestHelpers/TestDatabaseContext/`:

**Key entities:**
- `TestEntity` - Primary entity with various property types, owned entities, relationships
- `TestEntityWithAutoIncrement` - Auto-increment testing
- `TestEntityWithRowVersion` - Concurrency token testing
- `TestEntityWithShadowProperties` - EF Core shadow properties
- `TestEntityWithSqlDefaultValues` - SQL-level defaults
- `TestEntityWithComplexType` - EF Core complex types
- `KeylessTestEntity` - Keyless entities/query types
- `OwnedEntity` and variants - Owned entity patterns

Each provider test project has its own `TestDbContext` in `TestDatabaseContext/TestDbContext.cs` that configures these entities. The test DbContext implements `IDbDefaultSchema` for schema testing.

Entity configuration is done via static `Configure(ModelBuilder)` methods on each entity class:
```csharp
public class TestEntity
{
    public static void Configure(ModelBuilder modelBuilder) { ... }
}
```

## Test Utility: String Extensions

For cross-platform test assertions on multiline strings:
```csharp
// Normalizes line breaks to the current environment
expectedSql.WithEnvironmentLineBreaks()
```
Located in `tests/Thinktecture.EntityFrameworkCore.TestHelpers/Extensions/StringExtensions.cs`.

## Common Assertion Patterns

```csharp
// Collection assertions
result.Should().HaveCount(3);
result.Should().BeEmpty();
result.Should().AllSatisfy(t => t.Name.Should().NotBeNullOrEmpty());

// String/SQL assertions
query.ToQueryString().Should().Be(expectedSql);
ExecutedCommands.Last().Should().Contain("FROM [dbo].[TestEntities]");

// Exception assertions
rootTx.Invoking(tx => tx.Commit())
    .Should().Throw<InvalidOperationException>()
    .WithMessage("This root transaction has completed*");

// Async exception assertions
await query.Awaiting(q => q.ToListAsync()).Should().NotThrowAsync();

// Boolean assertions
isTransactionUsable.Should().BeTrue();

// NSubstitute mock assertions
TenantDatabaseProviderMock.GetDatabaseName(Schema, "TestEntities").Returns(database);
```

## Warning Suppressions in Tests

Defined in `tests/Directory.Build.props`:
- `CA1062` - Validate arguments of public methods (too strict for test code)
- `EF1002` - Type doesn't implement EF design pattern members
- `xUnit1041` - Inferred test class name has non-standard suffix
- `CS1591` - Missing XML comments (per test project `.csproj`)
- `CA2000` - Dispose objects before losing scope (per test project `.csproj`)

## Checklist: Writing a New Test

1. Pick the right test project based on what you're testing (provider-specific or abstract)
2. Create a test class inheriting from the project's `IntegrationTestsBase`
3. Name methods starting with `Should_` describing the expected behavior
4. Use `ArrangeDbContext` for setup, `ActDbContext` for execution, `AssertDbContext` for verification
5. Use `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized tests
6. Verify generated SQL with `ExecutedCommands` or `ToQueryString()` where applicable
7. Use AwesomeAssertions fluent API (`.Should().Be(...)`) for all assertions
8. Return `async Task` (not `void`) for async test methods
