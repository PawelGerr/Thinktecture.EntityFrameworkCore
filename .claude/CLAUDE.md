# CLAUDE.md

All guidance for working with this repository is in this single file.

## Workflow

### Alignment Before Action
- For non-trivial tasks, do not jump straight into implementation. First summarize:
  (1) your understanding of the problem, (2) the proposed approach, (3) key trade-offs or risks.
  Keep this short (a few sentences, not an essay). Wait for confirmation before proceeding.
- **Act autonomously on clear bugs**: When the bug has a clear reproduction (failing test, error
  log, stack trace), go fix it — don't ask the user to guide each step. Still follow: write a
  failing test first, then fix, then validate. The alignment rule above still applies for ambiguous
  bugs where the root cause or intended behavior is unclear.
- When the user requests a different approach after seeing an implementation:
  briefly compare the old and new approaches — state the concrete pros and cons of each.
  If the new approach has significant drawbacks compared to the previous one, say so clearly before implementing.
  The user can still choose the new approach, but the decision should be informed.

### Context Management
- Offload codebase exploration and research to subagents (Explore/Plan types). The main context
  should contain decisions and results, not the search process.
- For multi-step implementations (e.g., feature + tests + adjustments), use subagents for isolated
  subtasks when the main context is already large. Prefer splitting into: (1) implement the feature,
  (2) write/adjust tests, (3) fix issues found in review.
- When the user requests adjustments or additional tests after an implementation, assess whether
  the main context has grown large. If so, delegate the follow-up work to a subagent with a clear
  description of what was implemented and what needs to change.

### Concurrency Awareness
This codebase has concurrency concerns in bulk operations, temp table management, and test
isolation. When modifying code in these areas:
- Identify shared mutable state and how it is protected (e.g., temp table suffix leasing,
  connection/transaction lifecycle in context factories).
- Consider whether new code respects existing isolation boundaries (e.g., SQL Server test lock
  tables, `DbContext` single-thread usage).
- When adding async code, consider: cancellation propagation, `IAsyncDisposable` cleanup ordering,
  and whether operations need to be atomic within a transaction scope.
- Remember that `DbContext` is not thread-safe — never share instances across threads.

### Self-Review Before Completion
Before presenting work as complete:
- Re-read the final state of all changed files (not from memory — actually re-read them).
- Check for: unused variables, missing null checks, inconsistent naming, unhandled edge cases.
- Simplicity check: for each new variable, ask whether it's necessary — does it name a complex
  expression, get reused, or aid debugging? If it only holds a value used once immediately, inline it.
  Apply the same scrutiny to wrapper methods, intermediate collections, and extra parameters.
- Verify the changes compile (`dotnet build -c Release`).
- Run affected tests (`dotnet test -c Release --no-build`, or a specific test project).
- For query translation changes: verify the generated SQL matches expectations using
  `CollectExecutedCommands()`.
- Final gut-check: "Would a staff engineer approve this?" — if anything feels over-engineered,
  unnecessarily clever, or under-justified, simplify before presenting.

### Planning
- Enter plan mode for tasks that touch 3+ files or require architectural decisions.
- If an approach hits a wall (repeated failures, wrong assumptions), stop and re-plan rather than
  pushing through. Do this immediately — do not attempt further fixes on a broken approach.
  Re-planning means stepping back to reassess the strategy, not retrying the same idea with tweaks.

### After Corrections
- When the user corrects a mistake that reflects a project-wide pattern or convention, update the
  relevant section of CLAUDE.md.
- Corrections must become **concrete rules in the relevant section**, not vague lessons. For example,
  if corrected about missing `CancellationToken` propagation, update the "EF Core Specifics" section
  with the rule — don't append a generic note somewhere else.

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
agent-skills/                           # Git submodule PawelGerr/agent-skills; this repo owns only skills/thinktecture-entityframeworkcore/
skills/thinktecture-entityframeworkcore/SKILL.md   # Redirect stub for old `npx skills add PawelGerr/Thinktecture.EntityFrameworkCore` installs; keep it, add nothing else
docs/                                   # Git submodule: GitHub wiki
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
9. **Keep documentation in sync** — After every substantial change, update this file, the user-facing `docs/` folder and the agent skill in `agent-skills/` (see [Documentation Maintenance](#documentation-maintenance))

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

### Abstractions in Internal Code

- Internal code paths receive the abstraction object (for example `IBulkOperationTableNameProvider`), never raw configuration values such as a table name and a schema. Overloads that take raw values exist only as public helpers that build the abstraction (for example `IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)`).
- Use BCL types (for example `Array.Empty<T>()` or `ReadOnlyDictionary<TKey, TValue>.Empty`) instead of custom private static instances such as a shared empty dictionary.

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
| **Temp Tables** | `ITempTableCreator`, `ITempTableReference`, `ITempTableQuery<T>`, `CreateTempTableAsync`, `BulkInsertIntoTempTableAsync`, `BulkInsertValuesIntoTempTableAsync` | Two workflows: all-in-one (create+insert via `BulkInsertIntoTempTableAsync`) or two-step (`CreateTempTableAsync` for empty table + insert later); auto-cleanup via `IAsyncDisposable`; name conflict prevention via `TempTableSuffixLeasing` |
| **Window Functions** | `EF.Functions.RowNumber()`, `.NTile()`, `.Average()`, etc. | Fluent `PartitionBy()`/`OrderBy()`; all three providers. `NTile` is per-provider (SQL Server requires ORDER BY and returns `long`/bigint; PG/SQLite return `int` and ORDER BY is optional) and lives in `{Provider}DbFunctionsExtensions`. |
| **Bitwise Aggregates** | `EF.Functions.BitOr`, `.BitAnd`, `.BitXor` | PostgreSQL only; GROUP BY aggregate over integer/`[Flags]`-enum columns; native `bit_or`/`bit_and`/`bit_xor`, no cast |
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
| Bitwise Aggregates (`bit_or`/`bit_and`/`bit_xor`) | - | Y | - |
| Collection Parameters | Y | Y | - |
| Nested Transactions | Y | Y | Y |
| Tenant Database Support | Y | - | - |
| Table Hints | Y | - | - |
| Bulk Update from Query | Y | Y | Y |
| Bulk Insert from Query | Y | Y | Y |
| Truncate Table (dedicated) | Y | Y | Y |

### PostgreSQL `max`/`min` over `uuid`

PostgreSQL has no native `max(uuid)`/`min(uuid)` aggregate (`max`/`min` over a `Guid`-mapped
`uuid` column translates but fails at execution: `function max(uuid) does not exist`). The Npgsql
provider auto-rewrites these to `max(col::text)::uuid` / `min(col::text)::uuid` (text ordering of
canonical uuid strings matches uuid byte ordering, so results are identical). **Always-on** (no
feature flag) via `NpgsqlAggregateMethodCallTranslatorPlugin` →
`NpgsqlUuidAggregateMethodCallTranslator` (`IAggregateMethodCallTranslator`), registered
unconditionally in `NpgsqlDbContextOptionsExtension.ApplyServices`. Triggers only when the
operand's `TypeMapping.StoreType == "uuid"` (covers `Guid` and `Guid?`; a `Guid`→`text`
value-converter column is left untouched); returns `null` on every other path so EF falls through
to the built-in translator. SQL Server (`uniqueidentifier`) and SQLite (Guid as text/blob) already
support `max`/`min` and are not touched.

The **windowed** `EF.Functions.Max`/`Min` over a `uuid` column (window functions feature) gets the
same treatment: `NpgsqlDbFunctionsTranslator.CreateWindowFunctionExpression` rewrites it to
`(max(col::text) OVER (...))::uuid` when `aggregateFunction is "MAX" or "MIN"` and the operand's
`TypeMapping.StoreType == "uuid"`. Only `MAX`/`MIN` are rewritten (`SUM`/`AVG` over a uuid have no
meaning and are left untouched). Without this, the windowed path emitted bare `max(uuid) OVER (...)`
and failed at runtime with the same `function max(uuid) does not exist` error as the aggregate path.

### PostgreSQL bitwise aggregates (`bit_or`/`bit_and`/`bit_xor`)

`EF.Functions.BitOr` / `BitAnd` / `BitXor` (in `NpgsqlAggregateDbFunctionsExtensions`, root
`Thinktecture` namespace) translate to PostgreSQL's native `bit_or`/`bit_and`/`bit_xor` GROUP BY
aggregates. Usage: `GroupBy(...).Select(g => EF.Functions.BitOr(g.Select(e => e.Col)))`. Generic
`BitOr<T>(IEnumerable<T>)` returning `T` — supports `short`/`int`/`long`, their nullable forms, and
`[Flags]` enums (backed by an integer type). **Always-on** (no feature flag) via
`NpgsqlBitwiseAggregateMethodCallTranslator` (`IAggregateMethodCallTranslator`), added to
`NpgsqlAggregateMethodCallTranslatorPlugin.Translators` alongside the uuid translator. Detection:
`method.DeclaringType == typeof(NpgsqlAggregateDbFunctionsExtensions)` then `switch(method.Name)`.
**No cast** — the aggregate reuses the group element selector's own `TypeMapping` and CLR return
type, so enums/ints round-trip (PostgreSQL `bit_*` returns the input type). Built with
`nullable: true` because `bit_*` returns `NULL` when every value in the group is `NULL` (nulls are
otherwise ignored). GROUP BY form only — no window/`OVER` form. `bit_xor` requires PostgreSQL 14+.

### Bulk Operation Details

- All return `Task<int>` (affected rows including owned entities)
- SQL Server uses `IEntityDataReader.RowsRead`; PostgreSQL uses `NpgsqlBinaryImporter.CompleteAsync()` return; SQLite uses `ExecuteNonQueryAsync()` return
- PostgreSQL resolves `NpgsqlDbType` per column via `INpgsqlTypeMapping` for correct type handling (e.g., `jsonb`/`json` from `string`)
- Table override via `TableNameProvider` (`IBulkOperationTableNameProvider.GetTableName(IEntityType)` returning `BulkOperationTableName(Name, Schema)`; `Schema == null` means database default, not model schema). Built-in: `IBulkOperationTableNameProvider.Default` (model tables; throws for temp table entities), `ForRootEntity(tableName, schema)` (root only; throws for separately mapped owned types), `ForRootEntityWithDefaultSchema(tableName)` (root only; fixed name, schema from the model via `entityType.GetSchema()`; throws for temp table entities; the obsolete `TableName` path does not), `ForRootEntityWithDefaultName(schema)` (root only; name from `Default`, so temp table entities throw; fixed schema, `null` emits no schema) and `Create(Func<IEntityType, BulkOperationTableName>)` (`DelegateTableNameProvider`; calls the delegate for the root and for every separately mapped owned type). Options `TableName`/`Schema` (also on the `*FromQueryOptions`) are `[Obsolete]`; their messages map `TableName` alone to `ForRootEntityWithDefaultSchema`, `Schema` alone to `ForRootEntityWithDefaultName` and both to `ForRootEntity`. Option classes delegate `TableName`, `Schema` and `TableNameProvider` to `Internal/BulkOperationTableNameOptions`: setting `TableName`/`Schema` replaces `TableNameProvider` with a root-only provider (`RootEntityTableNameProviderBase.FromObsoleteOptions`; a null part keeps the model value; it builds the same three root-only classes as the three `ForRootEntity*` factories, but without validation, so an empty part fails in the executor), setting `TableNameProvider` clears `TableName`/`Schema`, and the last assignment wins. `TableNameProvider` is non-nullable and defaults to `IBulkOperationTableNameProvider.Default`; setting it to `null` throws `ArgumentNullException`. The interface default member `TableNameProvider` builds the same provider for implementations that only implement `TableName`/`Schema`, or returns `Default` when both are null. Executors read `options.TableNameProvider` and never read `TableName`/`Schema`; the query-based operations take nullable options and read `options?.TableNameProvider ?? IBulkOperationTableNameProvider.Default`. Executors look up the entity type via `GetEntityTypeOrTempTableEntityType<T>()`, resolve the root table via `BulkOperationTableNameProviderExtensions.GetValidTableName(entityType)` and pass the provider down; the child loops call `tableNameProvider.GetValidTableName(childContext.EntityType)` when they reach an owned type. The library gives no atomicity guarantee: when the provider throws for an owned type, rows of the root table may already be written. SQLite rolls them back only when the executor started its own transaction. Atomicity is the responsibility of the caller's transaction. Temp table targets use `ForRootEntity(tempTable.Name)`; a user `TableNameProvider` is never applied to temp tables. Owned types in update/upsert: SQL Server/PostgreSQL reject every owned kind (temp table creation throws `NotSupportedException` "Temp tables don't support owned entities." before the target is written); SQLite update writes inlined, separate `OwnsOne` and `OwnsMany` via the child loop (root table needs a non-key column, otherwise empty `SET` and syntax error); SQLite upsert supports inlined only; the `BulkInsertOrUpdateContext` constructor throws `NotSupportedException` for separate owned types (no update properties, because update properties are inlined-only) before the first write. Tests: `Should_*owned_type*`/`Should_*owned_collection*` in the `BulkUpdateAsync`/`BulkInsertOrUpdateAsync` test classes of each provider. Property selection via `IEntityPropertiesProvider`.
- PostgreSQL temp table collation: strings with `.` split into schema+name by default; set `SplitCollationComponents = false` to escape as single identifier
- Temp table collation (SQL Server, PostgreSQL) is read from the design-time model (`IDesignTimeModel`), because the runtime model has no collation. `GetDesignTimeProperty` in `SqlServerTempTableCreator`/`NpgsqlTempTableCreator` finds a string member of a complex type through its `IComplexType.ComplexProperty` chain; `GetEntityType(property.DeclaringType.Name)` alone (the library extension `RelationalModelExtensions.GetEntityType(IModel, string)`) throws `Thinktecture.EntityFrameworkCore.EntityTypeNotFoundException` for complex types. SQLite has no collation lookup.
- **Complex types** (`ComplexProperty`, class or struct, nested allowed): `BulkInsertAsync`, temp tables (DDL and `tempTable.Query`) and the COPY/`SqlBulkCopy` paths use `GetFlattenedProperties()` and the configured column names, so they need no special code. Query-based bulk update/insert accept a target selector through complex properties (`.Set(e => e.Boundary.Upper, (e, f) => f.Boundary.Upper)`, `.Map(e => e.Boundary.Upper, f => f.Boundary.Upper)`) and a whole complex property (`.Set(e => e.Boundary, (e, f) => f.Boundary)`, also from a captured value or a `new` expression without a lambda parameter, which is evaluated on the client). `BulkUpdateFromQueryHelper.ExtractPropertyAccess` records the complex members in `PropertyAccessInfo.ComplexPath`, `ResolveProperty` walks it via `FindComplexProperty`, and `ExpandComplexProperties` (called once per operation by `EfCoreValueExpressionTranslator`) replaces a whole-complex entry with one entry per member, recursively, by appending the member access to the target and the value selector. Members with a value converter work in all these paths; a captured value is emitted with the converter of the target column. `ExpandComplexProperties` skips generated members (`GetBeforeSaveBehavior() == Ignore`, e.g. computed columns), the same rule as `IEntityPropertiesProvider`; an explicit `Set`/`Map` of such a member still writes it, and a whole complex property whose writable members are all generated throws `ArgumentException` ("Expression: <that selector>", checked per entry, also when other `Set`/`Map` entries exist). Duplicate target columns (also a whole complex property plus one of its members) throw `ArgumentException` on all providers, checked in both translator loops with a `HashSet<IProperty>`. A captured `null` that is dereferenced during client evaluation (a whole complex value, or a member access on a null captured object, also in filters) throws `ArgumentException` from `ClosureFlatteningVisitor`; a null guard (`captured == null ? 0 : captured.Count`, `nullableInt.HasValue ? 1 : 2`, also `captured == null || e.Name == captured.Name`) prevents the exception, see the "Value expression evaluation" bullet. `EF.Property` must be called on the lambda parameter (`NotSupportedException`). `ResolveProperty` names the declaring type (the complex type for a complex member) in its `InvalidOperationException`. Not supported: complex collections (`NotSupportedException`), a nullable complex property as a whole (`NotSupportedException`), a whole-property value of a nullable type such as a captured `TimestampPair?` (`NotSupportedException`), and a member through a nullable complex property, class or struct (`e.OptionalBoundary!.Upper`, `e.EffectiveMessage!.Value.ReceivedAt`, `NotSupportedException` "The member '…' of '…' is a nullable complex property."; checked in `ResolveDeclaringType` before the next path element, so it covers `Set`, `Map` and nested paths). Shadow properties on complex types cannot exist in EF Core 10. EF Core cannot bind a nested complex type to a constructor parameter, so nested complex types need settable or init members. Tests: `*ComplexType*` and `*value_converted*` in the PostgreSQL bulk test classes, `Should_update_*ComplexType*` in the SQL Server and SQLite `BulkUpdateFromQueryAsync` classes.
- **Query-based bulk update** (all providers): `DbSet<TTarget>.BulkUpdateAsync(sourceQuery, targetKey, sourceKey, setBuilder, filter?, options?)`. SQL Server generates `UPDATE t SET ... FROM target INNER JOIN (sourceQuerySql) AS s ON ... [WHERE filterCondition]`; PostgreSQL/SQLite generate `UPDATE target AS t SET ... FROM (sourceQuerySql) AS s WHERE ... [AND filterCondition]`. Uses `SetPropertyBuilder<TTarget, TSource>` for fluent property assignment. Key selectors support single and composite keys (anonymous types). Source SQL and parameters extracted via `CreateDbCommand()`. Optional `filter` (`Expression<Func<TTarget, TSource, bool>>`) restricts which rows are updated; filter can reference both target and source properties (e.g., `(e, f) => e.Count < f.Count`); filter is translated by the same SQL translator as the SET values; a filter that translates to a constant `false` (e.g. `captured != null && ...` with a null `captured`, or EF folding `col > 0 && false`) is applied as `1 = 0`, because SQL Server rejects a bare `CAST(0 AS bit)` as a condition and the library does not run EF's search-condition conversion; a constant `true` is dropped by `ApplyPredicate`. Shared expression analysis logic in `BulkUpdateFromQueryHelper`. Table override via `TableNameProvider` on `SqlServerBulkUpdateFromQueryOptions`, `NpgsqlBulkUpdateFromQueryOptions`, or `SqliteBulkUpdateFromQueryOptions` (all implement `IBulkOperationFromQueryOptions`; obsolete `TableName`/`Schema` replace `TableNameProvider` through the same `BulkOperationTableNameOptions`; `EfCoreValueExpressionTranslator` receives the provider and resolves the target table only). Value expressions support constants, captured variables, and static members (e.g., `.Set(e => e.Count, (e, f) => 42)`, `.Set(e => e.Name, (e, f) => myVar)`, or `.Set(e => e.Status, (e, f) => Status.Published)`), complex arithmetic expressions referencing target and/or source properties (e.g., `.Set(e => e.Count, (e, f) => e.Count + f.Count * 2)` or `.Set(e => e.Count, (e, f) => f.Count + 10)`), in addition to simple property access and `EF.Property` calls. Captured values and static members are evaluated to constants and rendered as SQL literals (`SqlConstantExpression` with the target column's type mapping, so a value converter is applied); other expressions are translated by EF Core's `RelationalSqlTranslatingExpressionVisitor`. The only SQL parameters of the UPDATE come from the source query.
- **Query-based bulk insert** (all providers): `DbSet<TTarget>.BulkInsertAsync(sourceQuery, mapBuilder)`. Generates `INSERT INTO target (cols) SELECT s.cols FROM (sourceQuerySql) AS s` — identical structure across all three providers (only identifier quoting differs). Uses `InsertPropertyBuilder<TTarget, TSource>` for fluent column mapping via `Map(target => target.Prop, source => source.Prop)` with single-parameter value selector. No key selectors needed (insert, not join). Table override via `TableNameProvider` on `SqlServerBulkInsertFromQueryOptions`, `NpgsqlBulkInsertFromQueryOptions`, or `SqliteBulkInsertFromQueryOptions` (`IBulkOperationFromQueryOptions`, same as bulk update from query). Value selectors support constants, captured variables, and static members (e.g., `.Map(e => e.Count, f => 42)` or `.Map(e => e.Name, f => myVar)`), complex arithmetic expressions referencing source properties (e.g., `.Map(e => e.Count, f => f.Count * 2 + 1)`), in addition to simple property access and `EF.Property` calls. A value selector that is a single constant after client evaluation (a constant, a captured variable, a static member, `captured == null ? 0 : captured.Count`, a `new` expression) is bound as a parameter `__insert_const_N` (value converter applied via `ConvertToProvider`), so `ApplyProjection` does not merge equal constants; constants inside a larger expression are rendered as SQL literals; other expressions are translated by EF Core's `RelationalSqlTranslatingExpressionVisitor`.

- **Value expression evaluation** (query-based bulk update/insert): `FlattenClosures` in `EfCoreValueExpressionTranslator` runs two passes. `EvaluatableSubtreeFinder` marks every node whose subtree contains no `ParameterExpression`, no `LambdaExpression` and no extension node (the entity shapers that replace the lambda parameters) and whose nodes all pass `IEvaluatableExpressionFilter`, so server-translatable members (e.g. `DateTime.Now`) stay untouched. It never marks a method call whose first parameter is `DbFunctions` (`EF.Functions.RowNumber(EF.Functions.OrderBy(1))` has no client implementation) or a node whose type is assignable to `IQueryable` (a captured query is not executed during translation; such a value selector fails with "Could not translate value expression"). `ClosureFlatteningVisitor` replaces each topmost marked node (except a `ConstantExpression`) with a `ConstantExpression` by evaluating it once via `Expression.Lambda<Func<object?>>(Expression.Convert(node, typeof(object))).Compile(preferInterpretation: true)`. A subtree is evaluated as a whole, so closure fields, static members (e.g. smart-enum values like `Status.Published`), `nullableInt.HasValue ? 1 : 2`, `captured == null ? 0 : captured.Count` and `captured.Name.ToUpper()` follow C# semantics and become literals. Like EF Core's funcletizer, `ClosureFlatteningVisitor` short-circuits: a `ConditionalExpression` whose `Test` is marked is replaced by the visited selected branch (converted to the conditional's type if needed), and an `AndAlso`/`OrElse` without a custom `Method` whose `Left` is marked becomes `Constant(false)`/`Constant(true)` or the visited `Right` (a lifted `bool?` left operand that is `null` keeps the operator). So `captured == null || e.Name == captured.Name` and `captured == null ? f.Count : captured.Count + f.Count` work for a null `captured`. When the evaluation throws a `NullReferenceException`, or an `InvalidOperationException` (then only `Nullable<T>.Value` accesses are checked), `FindMemberOfNullInstance` looks for the innermost member access whose instance is `null`; if it finds one, the result is `ArgumentException` "The member '…' of '…' cannot be read because the captured value is null. Provide a non-null value. Expression: …" (also for `n.Value` on a null `int?`, which throws `InvalidOperationException` internally). Any other `NullReferenceException` becomes `ArgumentException` "The expression cannot be evaluated because a captured value is null. Expression: …", and every other exception becomes `InvalidOperationException` "The expression '…' cannot be evaluated on the client: …"; both keep the original exception as inner exception. Resulting `ConstantExpression`s bypass EF's SQL translator (which fails for types without a built-in type mapping) and are emitted directly with the target column's type mapping, so value-converter-typed values work; in the insert path, the converter is applied via `ConvertToProvider` before the value is bound as a raw parameter.

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
| Child loop resolves owned table from `entityType.GetTableName()` | `TableNameProvider` ignored for owned types; rows land in the model table | Call `tableNameProvider.GetValidTableName(childContext.EntityType)` instead |
| Reading `options.TableName`/`Schema` in an executor | CS0618 and a second configuration path | Read `options.TableNameProvider` once and pass the provider |
| `entityType.FindProperty(member)` for a query-based target selector | `e => e.Complex.Member` or `e => e.Complex` is rejected as "not a mapped property" | Resolve through `BulkUpdateFromQueryHelper` (`ComplexPath` + `ExpandComplexProperties`) |
| `EF.Property(e.Complex, "Member")` as a query-based target | `NotSupportedException` | Use member access `e.Complex.Member` |
| Design-time lookup by `property.DeclaringType.Name` | `Thinktecture.EntityFrameworkCore.EntityTypeNotFoundException` for a string member of a complex type in a temp table | Walk `IComplexType.ComplexProperty` up to the entity type (`GetDesignTimeProperty`) |

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
3. **Agent skill (`agent-skills/` submodule)** — Update the `references/*.md` page that mirrors each changed `docs/` page, and check `SKILL.md` (see [Agent Skill](#agent-skill))

### Docs Structure

- **Format:** GitHub Wiki markdown in `docs/` folder
- **File naming:** Title-Case-With-Hyphens (e.g., `Bulk-Insert.md`, `Table-Hints-(SQL-Server).md`)
- **Provider annotations:** parenthesized on sidebar entries, e.g., `(SQL Server, PostgreSQL)`
- **Sidebar organization** (`docs/_Sidebar.md`): sections are Performance, Features, Convenience, Integration Testing, Extensibility
- **PostgreSQL gap:** Docs predate the PostgreSQL provider — when touching any page, check for missing PostgreSQL examples

### Agent Skill

The agent skill `thinktecture-entityframeworkcore` lives in the `agent-skills/` submodule (repo `PawelGerr/agent-skills`, branch `main`). Its `references/*.md` pages mirror the `docs/` pages, so a change to user-facing behavior updates both.

- `SKILL.md` is the entry point and summarizes behavior itself: the pitfalls table (§5, one row per exception or misuse with its fix and a reference link), the feature catalog (§3), the approach guide (§4) and the provider feature matrix (§6). A change that adds or changes an exception, an option, a factory, or a provider capability updates the matching row there as well, not only the `references/*.md` page. `SKILL.md` has CRLF line endings; keep them.
- Edit only `agent-skills/skills/thinktecture-entityframeworkcore/`, its entry in `agent-skills/.claude-plugin/marketplace.json`, and its row in `agent-skills/README.md`. Never touch `agent-skills/skills/thinktecture-runtime-extensions/`; that folder belongs to the Thinktecture.Runtime.Extensions repo.
- Commit in `agent-skills` first (normal commit on top of `main`, never amend or force-push), then commit the updated gitlink in this repo. `push.recurseSubmodules=on-demand` pushes `agent-skills` before this repo.
- Verify with `claude plugin validate ./agent-skills`.

**Skip** for: purely internal refactoring, trivial fixes, test-only changes without infrastructure changes.

## CI/CD

- GitHub Actions on every push (`.github/workflows/main.yml`)
- Build/test on Ubuntu with `dotnet build -c Release` / `dotnet test -c Release --no-build`
- SQL Server: `UseSqlServerContainer=true`; PostgreSQL: `UsePostgreSqlContainer=true`
- NuGet published to nuget.org on git tags
- Test results: `test-results/<TargetFramework>/` as TRX files
