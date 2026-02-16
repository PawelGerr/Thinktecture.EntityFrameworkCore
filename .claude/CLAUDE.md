# CLAUDE.md

All guidance for working with this repository is in this single file.

## Quick Reference

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
dotnet test tests/Thinktecture.EntityFrameworkCore.SqlServer.Tests/ -c Release   # single project
```

| Aspect | Value |
|--------|-------|
| Target framework | `net10.0` |
| Language | C# 14.0, nullable enabled, implicit usings |
| EF Core | 10.0.2 (`Microsoft.EntityFrameworkCore.*`) |
| PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 |
| Testing | xUnit 2.9.3 + AwesomeAssertions 9.3.0 + NSubstitute 5.3.0 |
| Logging | Serilog.Extensions.Logging 10.0.0 + Serilog.Sinks.XUnit 3.0.19 |
| SQL Server tests | Testcontainers.MsSql 4.10.0 (Docker) or configured connection string |
| PostgreSQL tests | Testcontainers.PostgreSql 4.10.0 (Docker) or configured connection string |
| Root namespace | `Thinktecture` (set in Directory.Build.props) |
| Package versions | Centralized in `Directory.Packages.props` (no versions in `.csproj`) |

### MCP Servers

| Server | Points To | When To Use |
|--------|-----------|-------------|
| `serena` | This project (Thinktecture.EntityFrameworkCore) | All code exploration and editing in this repo |
| `serena-efcore` | Microsoft.EntityFrameworkCore source code | Looking into EF Core's internal implementation (e.g., understanding base classes, extension points, service registration patterns) |

### Project Layout

```
src/                                    # Runtime packages (9 projects)
  Thinktecture.EntityFrameworkCore.Relational/          # Foundation layer
  Thinktecture.EntityFrameworkCore.BulkOperations/      # Provider-agnostic bulk ops
  Thinktecture.EntityFrameworkCore.SqlServer/            # SQL Server implementation
  Thinktecture.EntityFrameworkCore.SqlServer.Testing/    # SQL Server test utilities
  Thinktecture.EntityFrameworkCore.PostgreSQL/           # PostgreSQL implementation (Npgsql)
  Thinktecture.EntityFrameworkCore.Sqlite.Core/          # SQLite foundation
  Thinktecture.EntityFrameworkCore.Sqlite/               # SQLite full package
  Thinktecture.EntityFrameworkCore.Sqlite.Testing/       # SQLite test utilities
  Thinktecture.EntityFrameworkCore.Testing/              # Shared testing infrastructure
tests/                                  # Test projects (7 projects, mirror src/)
  Thinktecture.EntityFrameworkCore.TestHelpers/          # Shared test entities & DbContext
samples/                                # Sample apps and benchmarks
```

### Internal Provider Project Structure

Each provider project follows this directory layout:

```
EntityFrameworkCore/
  BulkOperations/              # Provider-specific bulk op implementations
  Infrastructure/              # DbContextOptionsExtension, ExtensionInfo, SingletonOptions
  Migrations/                  # Migration SQL generators
  Parameters/                  # Collection parameter implementations
  Query/                       # Query translation visitors, SQL generators
    ExpressionTranslators/     # IMethodCallTranslatorPlugin implementations
  TempTables/                  # Temp table creators and references
Extensions/                    # Public extension methods (Thinktecture namespace)
```

The Relational foundation adds: `Data/` (entity data readers), `Internal/` (annotations), `Storage/` (nested transactions), `Linq/Expressions/` (expression utilities).

### Package Dependency Graph

```
Relational (foundation)
  └─ BulkOperations (abstractions)
       ├─ SqlServer (SQL Server impl) → SqlServer.Testing
       ├─ PostgreSQL (PostgreSQL/Npgsql impl)
       └─ Sqlite.Core → Sqlite → Sqlite.Testing
Testing (shared test infrastructure, used by *.Testing packages)
```

### Key Configuration Files

| File | Purpose |
|------|---------|
| `Thinktecture.EntityFrameworkCore.slnx` | Solution file (19 projects) |
| `Directory.Build.props` | Root MSBuild props (version, TFM, lang version) |
| `Directory.Packages.props` | Centralized NuGet versions |
| `src/Directory.Build.props` | Source Link, symbol packages |
| `tests/Directory.Build.props` | Test dependencies, warning suppressions, global usings |
| `global.json` | .NET SDK 10.0.0, rollForward: latestMajor |

## Critical Rules

1. **Always async with CancellationToken** — Every I/O method: `CancellationToken cancellationToken = default` as last param; pass through to all awaited calls
2. **Server-evaluable queries** — Never force client evaluation; keep operations in `IQueryable<T>`
3. **No raw SQL concatenation** — Use `FromSqlInterpolated` or parameterized helpers
4. **Internal by default** — Only `public` when needed across package boundaries; use `sealed` on non-inheritable classes
5. **Null checks** — `ArgumentNullException.ThrowIfNull()` for parameters (preferred); `?? throw new ArgumentNullException(nameof(...))` for constructor assignments
6. **XML documentation** — All public APIs must have XML docs with `<summary>`, `<param>`, `<typeparam>`, `<returns>`, `<exception>` as applicable
7. **No version numbers in PackageReference** — Versions centralized in `Directory.Packages.props`
8. **File-scoped namespaces** — All files use `namespace Thinktecture...;` syntax
9. **Keep documentation in sync** — After every substantial change, update this file and user-facing `docs/` folder

## Code Style

### Namespaces

- **Public extension methods** go in root `Thinktecture` namespace with `// ReSharper disable once CheckNamespace`
- **Internal types** use full feature-specific namespace (e.g., `Thinktecture.EntityFrameworkCore.BulkOperations`)
- `Microsoft.EntityFrameworkCore` is globally imported via Directory.Build.props

### Visibility

- `public` — Interfaces (API contracts), extension method classes, option types, enums, exceptions
- `internal sealed` — Implementation classes, factories, translators, visitors
- No `InternalsVisibleTo` between projects

### Naming & Types

- Extension method classes: `{Feature}{Target}Extensions` (static class, in `Thinktecture` namespace)
- Options: strongly-typed sealed classes implementing feature-specific interfaces (e.g., `SqlServerBulkInsertOptions : IBulkInsertOptions`)
- Records for simple data-transfer types; readonly structs for value types with custom equality
- Stateless implementations use singleton instance pattern: `public static readonly IFoo Instance = new Foo();` with private ctor

### C# 14 `field` Keyword

Prefer the C# 14 `field` keyword over explicit backing fields for lazy initialization and null-coalescing patterns:

```csharp
private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();
```


### Error Handling

- `ArgumentNullException` (null params), `ArgumentException` (invalid values), `InvalidOperationException` (invalid state), `NotSupportedException`
- Custom exceptions use `[CallerArgumentExpression]` where appropriate

### DI Registration

- Feature registration via `DbContextOptionsBuilder` extension methods (`AddBulkOperationSupport()`, etc.)
- Internal DI: `AddXxx` naming, match EF Core's expected `ServiceLifetime` via `GetLifetime<TService>()`

### Expression-Bodied Members

- Use for simple one-liner properties/methods; prefer full bodies for anything non-trivial

### Suppressions

- **Global**: `CA1303`, `MSB3884`
- **Tests only**: `CA1062`, `EF1002`, `xUnit1041`, `CS1591`, `CA2000`

## Architecture

### Core Features

| Feature | Key Types | Notes |
|---------|-----------|-------|
| **Bulk Operations** | `IBulkInsertExecutor`, `IBulkUpdateExecutor`, `IBulkInsertOrUpdateExecutor` | SQL Server: `SqlBulkCopy`/MERGE; PostgreSQL: COPY protocol/INSERT ON CONFLICT; SQLite: batched INSERT/UPDATE |
| **Bulk Update from Query** | `SqlServerBulkOperationsDbSetExtensions.BulkUpdateAsync`, `NpgsqlBulkOperationsDbSetExtensions.BulkUpdateAsync`, `SqliteBulkOperationsDbSetExtensions.BulkUpdateAsync`, `SetPropertyBuilder<TTarget, TSource>` | All three providers; SQL Server: `UPDATE t SET ... FROM target INNER JOIN (subquery) AS s ON ...`; PostgreSQL/SQLite: `UPDATE target AS t SET ... FROM (subquery) AS s WHERE ...`; fluent `Set` builder; accepts `IQueryable<TSource>` as source; optional `filter` parameter (`Expression<Func<TTarget, TSource, bool>>`) restricts which rows are updated (can reference both target and source properties) |
| **Bulk Insert from Query** | `SqlServerBulkOperationsDbSetExtensions.BulkInsertAsync`, `NpgsqlBulkOperationsDbSetExtensions.BulkInsertAsync`, `SqliteBulkOperationsDbSetExtensions.BulkInsertAsync`, `InsertPropertyBuilder<TTarget, TSource>` | All three providers; generates `INSERT INTO target (cols) SELECT s.cols FROM (subquery) AS s`; fluent `Map` builder; accepts `IQueryable<TSource>` as source |
| **Temp Tables** | `ITempTableCreator`, `ITempTableReference`, `ITempTableQuery<T>` | Queryable wrapper with auto-cleanup; name conflict prevention via `TempTableSuffixLeasing` |
| **Window Functions** | `EF.Functions.RowNumber()`, `.Average()`, etc. | Fluent `PartitionBy()`/`OrderBy()`; all three providers |
| **Table Hints** | `query.WithTableHints(SqlServerTableHint.NoLock)` | SQL Server only |
| **Nested Transactions** | `NestedRelationalTransactionManager` | Root = real transaction; children = logical |
| **Collection Parameters** | `ScalarCollectionParameter<T>`, `JsonCollectionParameter`, `NpgsqlJsonCollectionParameter` | Provider-specific factories |
| **Tenant Databases** | `ITenantDatabaseProvider` | Per-tenant DB names; cache-key aware |

### Provider Feature Matrix

| Feature | SQL Server | PostgreSQL | SQLite |
|---------|:---:|:---:|:---:|
| Bulk Insert | Y | Y | Y |
| Bulk Update | Y | Y | Y |
| Bulk Insert-or-Update (Upsert) | Y | Y | Y |
| Temp Tables | Y | Y | Y |
| Window Functions | Y | Y | Y |
| Collection Parameters | Y | Y | - |
| Nested Transactions | Y | Y | Y |
| Tenant Database Support | Y | - | - |
| Table Hints | Y | - | - |
| Bulk Update from Query | Y | Y | Y |
| Bulk Insert from Query | Y | Y | Y |
| Truncate Table (dedicated) | Y | Y | Y |

### Bulk Operation Details

- All return `Task<int>` (affected rows including owned entities)
- SQL Server uses `IEntityDataReader.RowsRead`; PostgreSQL uses `NpgsqlBinaryImporter.CompleteAsync()` return; SQLite uses `ExecuteNonQueryAsync()` return
- PostgreSQL resolves `NpgsqlDbType` per column via `INpgsqlTypeMapping` for correct type handling (e.g., `jsonb`/`json` from `string`)
- Table/schema override via options; property selection via `IEntityPropertiesProvider`
- PostgreSQL temp table collation: strings with `.` split into schema+name by default; set `SplitCollationComponents = false` to escape as single identifier
- **Query-based bulk update** (all providers): `DbSet<TTarget>.BulkUpdateAsync(sourceQuery, targetKey, sourceKey, setBuilder, filter?, options?)`. SQL Server generates `UPDATE t SET ... FROM target INNER JOIN (sourceQuerySql) AS s ON ... [WHERE filterCondition]`; PostgreSQL/SQLite generate `UPDATE target AS t SET ... FROM (sourceQuerySql) AS s WHERE ... [AND filterCondition]`. Uses `SetPropertyBuilder<TTarget, TSource>` for fluent property assignment. Key selectors support single and composite keys (anonymous types). Source SQL and parameters extracted via `CreateDbCommand()`. Optional `filter` (`Expression<Func<TTarget, TSource, bool>>`) restricts which rows are updated; filter can reference both target and source properties (e.g., `(e, f) => e.Count < f.Count`); filter is translated via `ValueExpressionTranslator` (same instance as SET values), with unified `@__ev_N` parameters. Shared expression analysis logic in `BulkUpdateFromQueryHelper`. Table/schema override via `SqlServerBulkUpdateFromQueryOptions`, `NpgsqlBulkUpdateFromQueryOptions`, or `SqliteBulkUpdateFromQueryOptions`. Value expressions support constants and captured variables (e.g., `.Set(e => e.Count, (e, f) => 42)` or `.Set(e => e.Name, (e, f) => myVar)`), complex arithmetic expressions referencing target and/or source properties (e.g., `.Set(e => e.Count, (e, f) => e.Count + f.Count * 2)` or `.Set(e => e.Count, (e, f) => f.Count + 10)`), in addition to simple property access and `EF.Property` calls; constant values are emitted as SQL parameters (`@__bv_N`); complex expressions are translated to SQL via `ValueExpressionTranslator` with expression-value parameters (`@__ev_N`).
- **Query-based bulk insert** (all providers): `DbSet<TTarget>.BulkInsertAsync(sourceQuery, mapBuilder)`. Generates `INSERT INTO target (cols) SELECT s.cols FROM (sourceQuerySql) AS s` — identical structure across all three providers (only identifier quoting differs). Uses `InsertPropertyBuilder<TTarget, TSource>` for fluent column mapping via `Map(target => target.Prop, source => source.Prop)` with single-parameter value selector. No key selectors needed (insert, not join). Table/schema override via `SqlServerBulkInsertFromQueryOptions`, `NpgsqlBulkInsertFromQueryOptions`, or `SqliteBulkInsertFromQueryOptions`. Value selectors support constants and captured variables (e.g., `.Map(e => e.Count, f => 42)` or `.Map(e => e.Name, f => myVar)`), complex arithmetic expressions referencing source properties (e.g., `.Map(e => e.Count, f => f.Count * 2 + 1)`), in addition to simple property access and `EF.Property` calls; constant values are emitted as SQL parameters (`@__bv_N`); complex expressions are translated to SQL via `ValueExpressionTranslator` with expression-value parameters (`@__ev_N`).

- **Source entity type resolution** (query-based bulk update/insert): Source entity type is resolved via `FindEntityType(typeof(TSource))` first (regular CLR-type lookup), then falls back to `FindEntityType(EntityNameProvider.GetTempTableName(typeof(TSource)))` for temp-table-only entities registered via `ConfigureTempTableEntity<T>()`. Without the fallback, column name resolution uses property names instead of mapped column names, causing SQL errors.

### Extension Architecture

**Four-tier extension hierarchy:**
```
DbContextOptionsExtensionBase (shared utilities)
  ├─ RelationalDbContextOptionsExtension (provider-agnostic)
  ├─ SqlServerDbContextOptionsExtension
  ├─ NpgsqlDbContextOptionsExtension
  └─ SqliteDbContextOptionsExtension
```

Each has boolean feature flags with cascading dependencies (e.g., `AddBulkOperationSupport` auto-enables `AddCustomQueryableMethodTranslatingExpressionVisitorFactory`). Services registered conditionally in `ApplyServices()`.

**Component Decorator Pattern** — Non-destructively wraps EF Core's internal services via `RelationalDbContextComponentDecorator`:
- `IModelCustomizer` → `DefaultSchemaModelCustomizer<T>`
- `IModelCacheKeyFactory` → `DefaultSchemaRespectingModelCacheKeyFactory<T>`
- `IMigrationsAssembly` → `DefaultSchemaRespectingMigrationAssembly<T>`
- `IQueryContextFactory` → `ThinktectureRelationalQueryContextFactory<T>`

**Singleton Options Bridge** — `ISingletonOptions` bridges scoped `IDbContextOptionsExtension` config to singleton services.

### Query Translation Pipeline

```
LINQ → IQueryableMethodTranslatingExpressionVisitorFactory
  ├─ ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor
  └─ ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor

EF.Functions.* → IMethodCallTranslatorPlugin
  ├─ RelationalMethodCallTranslatorPlugin → RelationalDbFunctionsTranslator
  └─ NpgsqlMethodCallTranslatorPlugin → NpgsqlDbFunctionsTranslator

SQL Generation → IQuerySqlGeneratorFactory
  ├─ ThinktectureSqlServerQuerySqlGeneratorFactory
  └─ ThinktectureNpgsqlQuerySqlGeneratorFactory
```

### Implementation Recipes

Follow these steps when adding a new feature. Read the referenced template files for exact patterns.

**1. Feature Flag** — Add boolean flag to the provider's `DbContextOptionsExtension`. Use a simple auto-property for independent features. For flags that enable shared infrastructure, use a cascading computed getter that OR's dependent flags (e.g., `AddBulkOperationSupport || AddWindowFunctionsSupport`).
Template: `src/.../SqlServer/EntityFrameworkCore/Infrastructure/SqlServerDbContextOptionsExtension.cs`

**2. Extension Method** — Add user-facing method on the provider's `DbContextOptionsBuilder`. Use the private `AddOrUpdateExtension()` helper, which ensures `RelationalDbContextOptionsExtension` is added first via `TryAddExtension`, then applies a callback to set the flag.
Template: `src/.../SqlServer/Extensions/SqlServerDbContextOptionsBuilderExtensions.cs`

**3. ExtensionInfo** — Update the inner `ExtensionInfo` class in **all three methods** (forgetting any one causes silent bugs):

| Method | What to add | Bug if forgotten |
|--------|------------|------------------|
| `GetServiceProviderHashCode()` | `hashCode.Add(Extension.YourFlag)` | Flag changes ignored at runtime |
| `ShouldUseSameServiceProvider()` | `&& Extension.YourFlag == other.Extension.YourFlag` | Different configs share service provider |
| `PopulateDebugInfo()` | `debugInfo["Thinktecture:YourFlag"] = ...` | Missing from diagnostics |

**4. ApplyServices()** — Register services conditionally. Key helpers:
- `GetLifetime<TService>()` — resolves EF Core's expected `ServiceLifetime` for that service
- `AddWithCheck<TService, TImpl, TEFDefault>()` — replaces an EF Core service, validating the current registration matches the expected default
- `TryAddSingleton` + `AddSingleton` factory delegates for sharing one options instance across multiple interfaces

**5. ISingletonOptions Bridge** — If your flag affects a singleton service, add a property to the provider's `*Options : ISingletonOptions` class. Implement `Initialize()` (read flag once at startup) and `Validate()` (throw if flag changed between contexts).
Template: `src/.../Relational/EntityFrameworkCore/Infrastructure/RelationalDbContextOptionsExtensionOptions.cs`

**6. Implementation** — Choose the EF Core extension point:

| Feature Type | Extension Point | Template |
|-------------|----------------|----------|
| Custom LINQ method | `IQueryableMethodTranslatingExpressionVisitorFactory` | `src/.../SqlServer/.../Query/ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor.cs` |
| `EF.Functions.*` | `IMethodCallTranslatorPlugin` | `src/.../Relational/.../Query/ExpressionTranslators/RelationalMethodCallTranslatorPlugin.cs` |
| Custom SQL generation | `IQuerySqlGeneratorFactory` | `src/.../SqlServer/.../Query/ThinktectureSqlServerQuerySqlGenerator.cs` |
| Model conventions | `IConventionSetPlugin` | EF Core convention pipeline |
| Migration SQL | `IMigrationsSqlGenerator` | `src/.../SqlServer/.../Migrations/` |

Key patterns (read templates for full code):
- **Visitor**: Two constructors (normal + protected copy ctor), override `CreateSubqueryVisitor()` to return `new(this)`, override `VisitMethodCall()` chaining translators with `??` before `base`
- **Translator plugin**: Inject singleton options, conditionally add `IMethodCallTranslator` instances to `Translators` list based on feature flags

**7. Tests** — Add integration tests with SQL verification. See [Testing](#testing) for provider-specific templates.

**8. Docs** — Add/update pages in `docs/` and sidebar. See [Documentation Maintenance](#documentation-maintenance).

**9. Update CLAUDE.md** — Add feature to **Core Features** table, **Provider Feature Matrix**, and any affected sections.

## Common Pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Forgot `GetServiceProviderHashCode()` | Feature flag changes ignored at runtime | Add `hashCode.Add(Extension.YourFlag)` |
| Forgot `ShouldUseSameServiceProvider()` | Different configs share service provider | Add equality comparison for new flag |
| Forgot `PopulateDebugInfo()` | Missing from diagnostics (no runtime error) | Add `"Thinktecture:YourFlag"` debug entry |
| Missing `CreateSubqueryVisitor()` override | Custom visitor works for top-level queries but not subqueries | Override to return new instance using copy constructor |
| Cascading flag not wired | Flag set on extension but service never registered | Add flag to computed getter's OR chain |
| `ConfigureModel` set too late | Per-test model config ignored | Set `ConfigureModel` before first DbContext access |
| Missing `ISingletonOptions` validation | Changing flag between contexts silently ignored | Add Initialize/Validate pair in singleton options class |
| `FindEntityType(typeof(T))` for temp-table entities | Returns `null`, column names fall back to property names instead of mapped column names | Also check `FindEntityType(EntityNameProvider.GetTempTableName(typeof(T)))` — temp-table-only entities use a special name pattern |

## Testing

### Test Projects

| Test Project | Tests For | Database |
|-------------|-----------|----------|
| `Relational.Tests` | Relational package | SQLite (in-memory) |
| `BulkOperations.Tests` | Bulk ops abstractions | SQLite (in-memory) |
| `SqlServer.Tests` | SQL Server features | SQL Server (Testcontainers or configured) |
| `PostgreSQL.Tests` | PostgreSQL features | PostgreSQL (Testcontainers or configured) |
| `Sqlite.Tests` | SQLite features | SQLite (in-memory) |
| `Testing.Tests` | Testing infrastructure | Varies |
| `TestHelpers` | Shared entities & DbContext (not a test project) | N/A |

### Naming & Structure

- Test methods: `Should_DoSomething` or `Should_DoSomething_When_Condition`
- `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized
- Always `async Task` return (not `void`)

### AAA Pattern with Three DbContexts

All integration tests use `ArrangeDbContext` (setup), `ActDbContext` (execute), `AssertDbContext` (verify) — same database, independent change trackers, provided by `TestCtxProvider`.

### Provider-Specific Test Templates

#### SQL Server

```csharp
[Collection("SqlServerTests")]
public class IntegrationTestsBase : SqlServerDbContextIntegrationTests<TestDbContext>
{
   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : this(sqlServerFixture.ConnectionString, testOutputHelper,
              ITestIsolationOptions.DeleteData(NonExistingTableFilter)) { }
}
```

- Inherits `SqlServerDbContextIntegrationTests<TestDbContext>` (from `SqlServer.Testing`)
- `ArrangeDbContext`, `ActDbContext`, `AssertDbContext` accessed via `TestCtxProvider` property
- `ExecutedCommands` accessed via `TestCtxProvider.ExecutedCommands`
- Per-test config: `ConfigureModel` and `Configure` properties set in builder's `InitializeContext()`
- Schema isolation: `OneTimeMigrationStrategy`, shared schema

**Template:** `tests/Thinktecture.EntityFrameworkCore.SqlServer.Tests/IntegrationTestsBase.cs`

#### PostgreSQL

```csharp
[Collection("NpgsqlTests")]
public class IntegrationTestsBase : IAsyncLifetime, IAsyncDisposable
{
   private TestDbContext? _arrangeDbContext;
   private TestDbContext? _actDbContext;
   private TestDbContext? _assertDbContext;

   protected TestDbContext ArrangeDbContext => _arrangeDbContext ??= CreateDbContext();
   protected TestDbContext ActDbContext => _actDbContext ??= CreateDbContext();
   protected TestDbContext AssertDbContext => _assertDbContext ??= CreateDbContext();

   protected IntegrationTestsBase(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture) { ... }
}
```

- Does **not** inherit a base testing class — implements `IAsyncLifetime, IAsyncDisposable` directly
- DbContexts are lazy via nullable backing fields with null-coalescing
- `ExecutedCommands` captured via `CommandCapturingInterceptor.Commands` (added to options with `.AddInterceptors()`)
- Per-test config: `ConfigureModel` and `Configure` properties set on `TestDbContext` during `CreateDbContext()`
- Schema isolation: per-test schema created in `InitializeAsync()`, tables truncated before each test
- `DisposeAsync()` disposes all three DbContexts

**Template:** `tests/Thinktecture.EntityFrameworkCore.PostgreSQL.Tests/IntegrationTestsBase.cs`

#### SQLite

```csharp
public abstract class IntegrationTestsBase : IAssemblyFixture<DbContextProviderFactoryFixture>
{
   protected IntegrationTestsBase(
      ITestOutputHelper testOutputHelper,
      DbContextProviderFactoryFixture providerFactoryFixture) { ... }
}
```

- Uses `IAssemblyFixture<DbContextProviderFactoryFixture>` for shared factory
- `ArrangeDbContext`, `ActDbContext`, `AssertDbContext` accessed via `TestCtxProvider` (from fixture)
- Per-test config: `ConfigureModel` set in builder's `InitializeContext()`
- No collection attribute (assembly fixture handles sharing)

**Template:** `tests/Thinktecture.EntityFrameworkCore.Sqlite.Tests/IntegrationTestsBase.cs`

### How to Access the SUT and Services in Tests

| Need | How |
|------|-----|
| Bulk operation executor | `ActDbContext.GetService<IBulkInsertExecutor>()` (or provider-specific type) |
| Temp table creator | `ActDbContext.GetService<ITempTableCreator>()` |
| Per-test model config | Set `ConfigureModel = modelBuilder => { ... };` **before** first DbContext access |
| Per-test options config | Set `Configure = optionsBuilder => { ... };` **before** first DbContext access |
| Capture executed SQL | SQL Server/SQLite: `TestCtxProvider.ExecutedCommands`; PostgreSQL: `_commandCapturingInterceptor.Commands` |
| Verify SQL (post-exec) | `ExecutedCommands.Last().Should().Contain("expected SQL")` |
| Verify SQL (pre-exec) | `query.ToQueryString().Should().Contain("expected SQL")` |

### SQL Verification

```csharp
// Post-execution (via CollectExecutedCommands())
ExecutedCommands.Last().Should().Contain("WITH (NOLOCK)");

// Pre-execution
query.ToQueryString().Should().Contain("WITH (NOLOCK)");
```

### Custom Model Configuration

Override per-test: `ConfigureModel = modelBuilder => { ... };`

This must be set **before** the first access to `ArrangeDbContext`/`ActDbContext`/`AssertDbContext`. It is invoked inside `TestDbContext.OnModelCreating()` after standard entity configuration.

### Test Entities

Shared in `tests/Thinktecture.EntityFrameworkCore.TestHelpers/TestDatabaseContext/`: `TestEntity`, `TestEntityWithAutoIncrement`, `TestEntityWithRowVersion`, `TestEntityWithShadowProperties`, `TestEntityWithSqlDefaultValues`, `TestEntityWithComplexType`, `KeylessTestEntity`, `TestEntityWithJsonColumns`, `OwnedEntity` and variants. Each has a static `Configure(ModelBuilder)` method. Each provider's test project has its own `TestDbContext`.

### Connection Configuration

- SQL Server: `appsettings.json` with `UseSqlServerContainer` + `ConnectionStrings:default`
- PostgreSQL: `appsettings.json` with `UsePostgreSqlContainer` + `ConnectionStrings:default`
- SQL Server uses `OneTimeMigrationStrategy`; PostgreSQL uses `EnsureCreatedAsync` with schema isolation

### Test Utilities

- `expectedSql.WithEnvironmentLineBreaks()` — cross-platform multiline string normalization
- Global usings in test projects: `AwesomeAssertions`, `NSubstitute`, `Xunit`, `Xunit.Abstractions`

## Documentation Maintenance

After substantial changes (new features, API changes, new entities/options, changed behavior, new provider support, architectural changes), update:

1. **This file (`CLAUDE.md`)** — Keep tables, rules, and patterns accurate
2. **User-facing docs (`docs/` folder)** — Update existing docs for API changes; add new docs for new features; add PostgreSQL examples where applicable (docs predate PostgreSQL provider); update `docs/_Sidebar.md` for new pages

### Docs Structure

- **Format:** GitHub Wiki markdown in `docs/` folder
- **File naming:** Title-Case-With-Hyphens (e.g., `Bulk-Insert.md`, `Table-Hints-(SQL-Server).md`)
- **Provider annotations:** parenthesized on sidebar entries, e.g., `(SQL Server, PostgreSQL)`
- **Sidebar organization** (`docs/_Sidebar.md`): sections are Performance, Features, Convenience, Integration Testing, Extensibility
- **PostgreSQL gap:** Docs predate the PostgreSQL provider — when touching any page, check for missing PostgreSQL examples

**Skip** for: purely internal refactoring, trivial fixes, test-only changes without infrastructure changes.

## CI/CD

- GitHub Actions on every push (`.github/workflows/main.yml`)
- Build/test on Ubuntu with `dotnet build -c Release` / `dotnet test -c Release --no-build`
- SQL Server: `UseSqlServerContainer=true`; PostgreSQL: `UsePostgreSqlContainer=true`
- NuGet published to nuget.org on git tags
- Test results: `test-results/<TargetFramework>/` as TRX files
