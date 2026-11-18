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

### Branches
- `master` targets the newest EF Core version. A release branch `releases/<version>.x` (for example `releases/10.0.x`) targets an older EF Core version.
- A fix for a defect that exists on a release branch and on `master` is committed on the release branch first. Then the release branch is merged into `master`, and the merge commit contains the adaptations for `master`. Do not cherry-pick the fix, because every fix commit must exist only once in the history.
- A correction of a fix that is not pushed yet is amended into the fix commit on the release branch, and the merge into `master` is made again from the previous `master` commit. Do not add a correction commit and a second merge, so that the history has one fix commit and one merge.

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
| EF Core | 10.0.14 (`Microsoft.EntityFrameworkCore.*`) |
| PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 |
| MySQL | Microting.EntityFrameworkCore.MySql 10.0.14 (MySqlConnector); tests use MySQL 8.4, MariaDB is not tested |
| Testing | xUnit 2.9.3 + AwesomeAssertions 9.6.0 + NSubstitute 5.3.0 |
| Logging | Serilog.Extensions.Logging 10.0.0 + Serilog.Sinks.XUnit 3.0.19 |
| SQL Server tests | Testcontainers.MsSql 4.16.0 (Docker) or configured connection string |
| PostgreSQL tests | Testcontainers.PostgreSql 4.16.0 (Docker) or configured connection string |
| MySQL tests | Testcontainers.MySql 4.16.0 (Docker, image `mysql:8.4` with `--local-infile=1`) or configured connection string |
| Root namespace | `Thinktecture` (set in Directory.Build.props) |
| Package versions | Centralized in `Directory.Packages.props` (no versions in `.csproj`) |

### MCP Servers

| Server | Points To | When To Use |
|--------|-----------|-------------|
| `serena` | This project (Thinktecture.EntityFrameworkCore) | All code exploration and editing in this repo |
| `serena-efcore` | Microsoft.EntityFrameworkCore source code | Looking into EF Core's internal implementation (e.g., understanding base classes, extension points, service registration patterns) |

### Project Layout

```
src/                                    # Runtime packages (10 projects)
  Thinktecture.EntityFrameworkCore.Relational/          # Foundation layer
  Thinktecture.EntityFrameworkCore.BulkOperations/      # Provider-agnostic bulk ops
  Thinktecture.EntityFrameworkCore.SqlServer/            # SQL Server implementation
  Thinktecture.EntityFrameworkCore.SqlServer.Testing/    # SQL Server test utilities
  Thinktecture.EntityFrameworkCore.PostgreSQL/           # PostgreSQL implementation (Npgsql)
  Thinktecture.EntityFrameworkCore.MySql/                # MySQL implementation (Microting; MariaDB is not tested)
  Thinktecture.EntityFrameworkCore.Sqlite.Core/          # SQLite foundation
  Thinktecture.EntityFrameworkCore.Sqlite/               # SQLite full package
  Thinktecture.EntityFrameworkCore.Sqlite.Testing/       # SQLite test utilities
  Thinktecture.EntityFrameworkCore.Testing/              # Shared testing infrastructure
tests/                                  # Test projects (8 projects, mirror src/)
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
       ├─ MySql (MySQL/Microting impl)
       └─ Sqlite.Core → Sqlite → Sqlite.Testing
Testing (shared test infrastructure, used by *.Testing packages)
```

### Key Configuration Files

| File | Purpose |
|------|---------|
| `Thinktecture.EntityFrameworkCore.slnx` | Solution file (21 projects) |
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

### EF Core Internal API

- Use the public EF Core API first.
- Internal EF Core API (warning `EF1001`, "pubternal" types and members) is legitimate when the public API cannot do the job, or when the public API causes larger or different problems, for example a derived table in the generated SQL or a second translation path. Suppress the warning at the usage with `[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]`.

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
| **Bulk Operations** | `IBulkInsertExecutor`, `IBulkUpdateExecutor`, `IBulkInsertOrUpdateExecutor` | SQL Server: `SqlBulkCopy`/MERGE; PostgreSQL: COPY protocol/INSERT ON CONFLICT; MySQL: prepared multi-row INSERT or `MySqlBulkCopy`, update and upsert through a staging temp table (`UPDATE ... JOIN`, `DELETE`, `INSERT ... SELECT`); SQLite: batched INSERT/UPDATE |
| **Bulk Update from Query** | `SqlServerBulkOperationsDbSetExtensions.BulkUpdateAsync`, `NpgsqlBulkOperationsDbSetExtensions.BulkUpdateAsync`, `MySqlBulkOperationsDbSetExtensions.BulkUpdateAsync`, `SqliteBulkOperationsDbSetExtensions.BulkUpdateAsync`, `SetPropertyBuilder<TTarget, TSource>` | All four providers; SQL Server: `UPDATE t SET ... FROM target INNER JOIN (subquery) AS s ON ...`; PostgreSQL/SQLite: `UPDATE target AS t SET ... FROM (subquery) AS s WHERE ...`; MySQL: `UPDATE target AS t INNER JOIN <source> AS s ON ... SET ... [WHERE filter]`; fluent `Set` builder; accepts `IQueryable<TSource>` as source; optional `filter` parameter (`Expression<Func<TTarget, TSource, bool>>`) restricts which rows are updated (can reference both target and source properties) |
| **Bulk Insert from Query** | `SqlServerBulkOperationsDbSetExtensions.BulkInsertAsync`, `NpgsqlBulkOperationsDbSetExtensions.BulkInsertAsync`, `MySqlBulkOperationsDbSetExtensions.BulkInsertAsync`, `SqliteBulkOperationsDbSetExtensions.BulkInsertAsync`, `InsertPropertyBuilder<TTarget, TSource>` | All four providers; generates `INSERT INTO target (cols) SELECT s.cols FROM (subquery) AS s`; fluent `Map` builder; accepts `IQueryable<TSource>` as source |
| **Temp Tables** | `ITempTableCreator`, `ITempTableReference`, `ITempTableQuery<T>`, `CreateTempTableAsync`, `BulkInsertIntoTempTableAsync`, `BulkInsertValuesIntoTempTableAsync` | Two workflows: all-in-one (create+insert via `BulkInsertIntoTempTableAsync`) or two-step (`CreateTempTableAsync` for empty table + insert later); auto-cleanup via `IAsyncDisposable`; name conflict prevention via `TempTableSuffixLeasing`; `BulkInsertValuesIntoTempTableAsync<TColumn1>` reads the values through `ValueDataReader`, which applies `GetValueConverter() ?? GetTypeMapping().Converter` to non-null values, the same rule as `PropertyGetterCache` |
| **Window Functions** | `EF.Functions.RowNumber()`, `.NTile()`, `.Average()`, etc. | Fluent `PartitionBy()`/`OrderBy()`; all four providers. `NTile` is per-provider (SQL Server requires ORDER BY and returns `long`/bigint; PG/MySQL/SQLite return `int` and ORDER BY is optional) and lives in `{Provider}DbFunctionsExtensions`. |
| **Bitwise Aggregates** | `EF.Functions.BitOr`, `.BitAnd`, `.BitXor` | PostgreSQL (native `bit_or`/`bit_and`/`bit_xor`, no cast) and MySQL (`BIT_OR`/`BIT_AND`/`BIT_XOR` cast back to the type of the input, NULL for an empty group); GROUP BY aggregate over integer/`[Flags]`-enum columns |
| **Table Hints** | `query.WithTableHints(SqlServerTableHint.NoLock)` | SQL Server only |
| **Nested Transactions** | `NestedRelationalTransactionManager` | Root = real transaction; children = logical |
| **Collection Parameters** | `ScalarCollectionParameter<T>`, `JsonCollectionParameter`, `NpgsqlJsonCollectionParameter` | Provider-specific factories (MySQL: `MySqlCollectionParameterFactory` with `JSON_TABLE`) |
| **Tenant Databases** | `ITenantDatabaseProvider` | Per-tenant DB names; cache-key aware |

### Provider Feature Matrix

| Feature | SQL Server | PostgreSQL | MySQL | SQLite |
|---------|:---:|:---:|:---:|:---:|
| Bulk Insert | Y | Y | Y | Y |
| Bulk Update | Y | Y | Y | Y |
| Bulk Insert-or-Update (Upsert) | Y | Y | Y | Y |
| Temp Tables | Y | Y | Y | Y |
| Window Functions | Y | Y | Y | Y |
| Bitwise Aggregates (`bit_or`/`bit_and`/`bit_xor`) | - | Y | Y | - |
| Collection Parameters | Y | Y | Y | - |
| Nested Transactions | Y | Y | Y | Y |
| Tenant Database Support | Y | - | - | - |
| Table Hints | Y | - | - | - |
| Bulk Update from Query | Y | Y | Y | Y |
| Bulk Insert from Query | Y | Y | Y | Y |
| Truncate Table (dedicated) | Y | Y | Y | Y |
| Migrations `IfExists`/`IfNotExists` | Y | Y | - | - |
| Testing package | Y | - | - | Y |

### MySQL specifics

- **Package and registration**: `Thinktecture.EntityFrameworkCore.MySql` builds on Microting.EntityFrameworkCore.MySql (MySqlConnector). The tests run against MySQL 8.4. MariaDB is not tested. Features are registered on `MySqlDbContextOptionsBuilder` with `AddBulkOperationSupport(addBulkOperationSupport = true, configureTempTablesForPrimitiveTypes = true, useBulkCopy = false, maxParametersPerStatement = 5_000)`, `AddWindowFunctionsSupport()`, `AddCollectionParameterSupport(jsonSerializerOptions)` and `AddCustomQueryableMethodTranslatingExpressionVisitorFactory()`. Nested transactions use the shared `DbContextOptionsBuilder.AddNestedTransactionSupport()`. `MySqlDbContextOptionsExtension.Validate` rejects window functions below MySQL 8.0 or MariaDB 10.2 and collection parameters (`JSON_TABLE`) below MySQL 8.0.4 or MariaDB 10.6. EF Core calls `Validate` when it builds the internal service provider, so the exception appears at the first use of the context, not in `UseMySql`. `MySqlDbContextOptionsExtensionOptions` (`ISingletonOptions`) carries `UseBulkCopy` and `MaxParametersPerStatement`. Both values take part in `GetServiceProviderHashCode` and `ShouldUseSameServiceProvider`, so a context with other values gets its own internal service provider; `Validate` of the singleton options throws `InvalidOperationException` only if the values differ within one service provider. `MySqlBulkOperationExecutor` implements all bulk operations. No bulk operation falls back to row-by-row statements or to another operation; when an operation cannot run, it throws.
- **Insert paths**: the registration decides the insert path for every insert of the bulk operations: bulk insert, temp tables and the staging tables of update and upsert. The default path is a multi-row `INSERT` with parameters. Rows per statement = `max(1, MaxParametersPerStatement / columns)`. `MaxParametersPerStatement` is between 1 and 65,535 (the limit of a prepared statement), otherwise `ArgumentOutOfRangeException`. `MaxParametersPerStatement` on `MySqlBulkInsertOptions`, `MySqlTempTableBulkInsertOptions`, `MySqlBulkUpdateOptions` and `MySqlBulkInsertOrUpdateOptions` overrides the value of the registration for one operation; bulk copy ignores it. The statement of a full batch is prepared when the first full batch runs and is executed again with new values for every further full batch; the last partial batch runs as a one-time statement. MySqlConnector keeps a prepared statement on the session, with the command text as key, until the connection is reset or closed. `DbCommand.Dispose` does not deallocate it, and the executor does not deallocate it either. Therefore each distinct INSERT text (table, column list, rows per statement) stays allocated on the session. With `ConnectionReset=true` (the default) MySqlConnector releases the statements when the pooled session is opened again, because it resets a session when the session leaves the pool. An idle session in the pool keeps its statements until then or until it is closed. On a connection that stays open, or with `ConnectionReset=false`, many inserts into tables with different names can reach the server limit `max_prepared_stmt_count` (16,382 by default, error 1461). The limit counts the prepared statements of all sessions of the server, also of the idle sessions in the pool. Parameters use the type mapping of the provider type of each column, because the entity data reader yields provider values. `useBulkCopy: true` streams the `IEntityDataReader` through `MySqlBulkCopyDataReader` into `MySqlBulkCopy.WriteToServerAsync(IDataReader)` (`LOAD DATA LOCAL INFILE`, no `DataTable`). The adapter is necessary, because `EntityDataReader.GetValues` throws `NotSupportedException` and MySqlConnector calls it for a reader that is not a `DbDataReader`. Bulk copy needs `AllowLoadLocalInfile=true` in the connection string (checked before the copy, `NotSupportedException` otherwise) and `local_infile=ON` on the server. Columns are mapped by name. `LOAD DATA LOCAL` turns errors into warnings, so the executor throws `InvalidOperationException` ("MySqlBulkCopy inserted n of m rows. ...") for a warning of the level `Warning` or `Error` and for a difference between `RowsInserted` and `IEntityDataReader.RowsRead`. A warning of the level `Note` (for example 1265 for a decimal that is rounded to the scale of the column) is ignored, because the INSERT path accepts the same value. With bulk copy, a duplicate key raises `MySqlException` "... only n was inserted" or "... only n were inserted" (not 1062). An exception of the source during streaming (the enumeration of the entities, a property getter or a value converter) reaches the caller unchanged, as on the INSERT path, and the connection and the transaction stay usable. `MySqlBulkCopyDataReader.Read` reads the next row with all its values, catches the exception, stores it in `SourceException` and returns `false`, so MySqlConnector ends the `LOAD DATA` statement without an error. `BulkCopyAsync` rethrows the stored exception with `ExceptionDispatchInfo` after `WriteToServerAsync` and before it checks the warnings and the row count. Reason: MySqlConnector wraps an exception that escapes from the reader into `MySqlException` "Error during LOAD DATA LOCAL INFILE" and marks the session as failed, because this exception has no SQLSTATE. The connection is then closed, the server rolls back the transaction of the caller, EF Core opens the connection again for the next statement, and that statement runs in autocommit mode without an error. The rows before the failing row are loaded. The own transaction of the executor rolls them back. A transaction of the caller keeps them until the caller rolls it back. An exception that MySqlConnector raises itself while it writes a value (`NotSupportedException` "Type ... not currently supported") still closes the connection. When `WriteToServerAsync` throws after the source failed, the caller gets the exception of MySqlConnector. Tests: `Should_rethrow_the_exception_of_the_source_and_not_leave_rows`, `Should_keep_the_connection_and_the_transaction_of_the_caller_if_the_source_throws`, `Should_keep_the_transaction_of_the_caller_if_the_source_throws_during_the_staging_fill`, `Should_rethrow_the_exception_of_a_value_converter_and_not_leave_rows`, `Should_rethrow_the_exception_of_the_source_when_inserting_into_a_temp_table`. A `YEAR` column throws `NotSupportedException` with bulk copy. A connection that is not a `MySqlConnection` throws `NotSupportedException` with bulk copy. With bulk copy, the command timeout (`CommandTimeout` of the options, otherwise the timeout of the context) is the `BulkCopyTimeout` of the single `LOAD DATA` statement, so it limits the whole load. On the INSERT path it limits each statement. When the server rejects `LOAD DATA LOCAL` (`local_infile=OFF`), MySqlConnector keeps the data reader of the failed copy in the static dictionary `MySqlBulkLoader.s_sources`, because it removes only a `Stream` source after a failure. Each failed call therefore keeps its reader and the entities in memory until the process ends. The executor cannot prevent this. It checks only `AllowLoadLocalInfile` of the connection string before the copy. `max_allowed_packet` is not estimated; a statement that is too large fails with the error of MySqlConnector. Without a transaction of the caller, insert, update and upsert run in their own transaction; inside a transaction of the caller (also an ambient `TransactionScope`) they use it. The insert resolves and validates the tables of the root and of every separately mapped owned type before the first write.
- **Staging (update and upsert)**: the rows are staged in a temporary table: ``CREATE TEMPORARY TABLE <name> (`__ord` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY[, UNIQUE (keys)]) SELECT <key and value columns> FROM <resolved target table> LIMIT 0``. The `SELECT` copies type, charset, collation, NOT NULL and DEFAULT of the target columns, so values are converted and keys are compared as in the target. It does not copy `AUTO_INCREMENT`, and a generated column becomes a plain column. `__ord` numbers the rows in input order and is the primary key, which `sql_require_primary_key=ON` demands also for temporary tables (error 3750 otherwise). The fill never maps `__ord`. The key index is `UNIQUE (keys)` or nothing. There is no key index when the operation is an upsert and a key column is store-generated (`GetValueGenerationStrategy(storeObject) == MySqlValueGenerationStrategy.IdentityColumn && MySqlPropertyExtensions.IsCompatibleAutoIncrementColumn(property)`), and when a key column has a TEXT, BLOB or JSON base type (`tinytext`, `text`, `mediumtext`, `longtext`, `tinyblob`, `blob`, `mediumblob`, `longblob`, `json`; `MySqlTempTableCreator.GetBaseType` reads the type name up to the first `(` or space, and `IsTextBlobOrJson` compares it). Otherwise `UNIQUE (keys)` rejects duplicate input keys with error 1062, for nullable and NOT NULL keys. Duplicates that the index does not detect: keys with a NULL part (NULL values are distinct in a UNIQUE index), TEXT/BLOB/JSON keys, and duplicate ids of an AUTO_INCREMENT key in upsert. Probed on MySQL 8.4: duplicates that match one target row update it once, are counted once in the result, and the row gets the values of one duplicate (the first in input order in the probe; MySQL does not document the order). In upsert, duplicates of id 0 are all inserted with generated ids, duplicates of a new id other than 0 fail with error 1062 at the target key, and duplicates with a NULL key part that match no row are all inserted when the target indexes allow it. With bulk copy the fill reports a duplicate key as `MySqlException` "... only n was inserted" or "... only n were inserted" instead of 1062. A key wider than 3,072 bytes fails with error 1071. The staging name is the leased temp table name of the entity (`<model table>_<n>`); a `TableNameProvider` target with exactly that name fails with error 1137, and a long name can fail with error 1059. An expression default of a staged column that references a column that is not staged fails the `CREATE` with error 1054. The staging statements (CREATE, UPDATE, DELETE, INSERT ... SELECT, DROP) run through EF Core (`Thinktecture.Internal.BulkOperationsSqlExecutor.ExecuteNonQueryAsync`), so logging, interceptors, the current transaction and the command timeout apply. `CommandTimeout` of the options is set on the context for the operation and restored afterwards. `UPDATE` and `DELETE` carry the hint `/*+ SET_VAR(sql_safe_updates = 0) */`, because MySQL rejects them with error 1175 under `sql_safe_updates`. MySQL supports the hint from 8.0.3 on; MariaDB and older MySQL versions do not, so there the statements can fail with error 1175 under `sql_safe_updates` (not tested). Key columns are compared with `=`, nullable key columns with `<=>`. `DROP TEMPORARY TABLE IF EXISTS` removes the staging table, also after an error and after a failed or cancelled `CREATE`, because the server may have created the table before the client saw the failure. Error 4091 skips the drop, because MySQL rejects every access to a temporary table inside XA. Error 1050 skips the drop, because the existing table with the leased name does not belong to the operation (for example a public temp table with `DropTableOnDispose = false`). Tests: `Should_drop_staging_table_when_create_fails_after_the_server_created_it`, `Should_keep_existing_temp_table_when_staging_create_fails_with_1050`.
- **Bulk update**: one ``UPDATE target AS d INNER JOIN staging AS s ON <keys> SET d.`col` = s.`col` ``. The result is the number of matched rows, also of unchanged rows. This is guaranteed, because the provider forces `UseAffectedRows=False` (it rewrites every connection string and throws for a `DbDataSource` whose connection string lacks `AllowUserVariables=True;UseAffectedRows=False`; verified in the decompiled `MySqlConnectionStringOptionsValidator`). `ArgumentException` "The number of properties to update cannot be 0." when no non-key property is left.
- **Upsert**: three statements. (1) `UPDATE target AS d INNER JOIN staging AS s ON <keys> SET ...`, skipped when no column is left to update. (2) `DELETE s FROM staging AS s INNER JOIN target AS d ON <keys>`. (3) ``INSERT INTO target (insert columns) SELECT ... FROM staging AS s ORDER BY s.`__ord` ``. `INSERT ... WHERE NOT EXISTS` is not used, because MySQL cannot reference a temporary target twice in one statement (error 1137). `ORDER BY __ord` keeps the input order, because InnoDB checks foreign keys row by row; without it a child row that sorts before its parent fails with error 1452. The return value is the number of matched rows of the UPDATE plus the number of inserted rows. The insert set and the update set are independent. Default update set when `PropertiesToUpdate` is `null`: `DeterminePropertiesForUpdate(entityType, true)` minus the primary key. Key properties are never set. An explicit `PropertiesToUpdate` may set the primary key. This rule is a deliberate difference to PostgreSQL, which sets the primary key column when `KeyProperties` is another key; tests `Should_not_update_columns_of_the_primary_key_by_default` and `Should_not_rewrite_the_primary_key_when_upserting_by_another_key`. Several new rows with a generated key (id 0) get generated ids. The key columns need no unique index in the target, because the rows are matched by a join (like MERGE). A unique violation on another index raises error 1062. Limits: an update of an existing row that references a new row of the same batch fails with error 1452, because the UPDATE runs before the INSERT (guard test); a concurrent insert between UPDATE and INSERT can raise error 1062 (like MERGE without HOLDLOCK); when the INSERT fails inside a transaction of the caller, the UPDATE stays in that transaction.
- **XA and owned types**: inside an XA transaction (`TransactionScope` with `UseXaTransactions=true`, the default, and the server default `xa_detach_on_prepare=ON`) MySQL rejects the staging `CREATE TEMPORARY TABLE` with error 4091. Bulk update and upsert convert it into `NotSupportedException` with the `MySqlException` as inner exception. The message names three alternatives: `UseXaTransactions=false`, a DbContext transaction, and `xa_detach_on_prepare=OFF`. The connection string is not checked, because staging works with `xa_detach_on_prepare=OFF`. The public temp table API does not convert error 4091. Bulk insert and `MySqlBulkCopy` work inside XA. Update and upsert reject owned types before the first statement: `NotSupportedException` "MySQL bulk update and upsert do not support owned entities mapped to separate tables." for separate tables, and "Temp tables don't support owned entities." for a selected property of an inlined owned type.
- **Truncate**: always `TRUNCATE TABLE`, never `DELETE`. The shared `ctx.TruncateTableAsync<T>()`/`(Type)` throws `InvalidOperationException` when a transaction is active (`throwOnTransaction: true`), because `TRUNCATE TABLE` commits it implicitly. The overloads `TruncateTableAsync<T>(bool throwOnTransaction, CancellationToken)` and `TruncateTableAsync(Type, bool throwOnTransaction, CancellationToken)` in `MySqlBulkOperationsDbContextExtensions` have no default for `throwOnTransaction`; `false` runs `TRUNCATE TABLE` inside the transaction, which MySQL commits implicitly. These overloads resolve `MySqlBulkOperationExecutor` directly, so a replaced `ITruncateTableExecutor` registration does not apply to them. Detected transactions: `CurrentTransaction`, `Transaction.Current` and an enlisted transaction. Not detected (committed without an error): a raw `START TRANSACTION` or `autocommit=0`, a direct `EnlistTransaction` with `UseXaTransactions=false`, and a `TransactionScope` without `TransactionScopeAsyncFlowOption.Enabled` after a thread switch. MySqlConnector rejects the command inside a `DbConnection.BeginTransaction()` that was not passed to `UseTransaction`. MySQL rejects the truncate of a table that a foreign key references with error 1701, also when the referencing table is empty; therefore the truncate of an owner with a separately mapped owned type always fails. Inside XA, MySQL rejects it with error 1399. A project that references the PostgreSQL package as well passes the argument by name (`throwOnTransaction: false`), because the call is otherwise ambiguous with `TruncateTableAsync(bool cascade)` (CS0121).
- **Schema**: MySQL has no schemas. The provider option `SchemaBehavior` (`MySqlSchemaBehavior.Throw`, the default, `Ignore`, `Translate`) is stored as `IMySqlOptions.SchemaNameTranslator` (`null` for `Throw`). `DelimitIdentifier(name, schema)` never emits a schema: `Ignore` drops it, `Translate` maps it into the object name. With `Throw`, every bulk operation, `TruncateTableAsync` and both query-based operations throw `NotSupportedException` ("... but MySQL has no schemas. Set 'SchemaBehavior' ...") before the first statement when the resolved schema is not empty, also for a schema of the model. Reason: the statement would otherwise address the current database.
- **Query-based bulk update (source with a subquery on the target)**: ``UPDATE target AS t INNER JOIN <source> AS s ON ... SET ... [WHERE filter]``; the source is a table or a derived table. A source query over the target table that contains a subquery on the target table (for example `ctx.Set<T>().Where(s => ctx.Set<T>().Any(o => o.Count > s.Count))` or `ctx.Set<T>().Where(s => ctx.Set<T>().Select(o => o.Id).Contains(s.Id))`) fails with error 1093 ("You can't specify target table '...' for update in FROM clause"). Reason: MySQL merges the derived table of the source into the `UPDATE`, and the subquery then reads the update target. A source over the target table without such a subquery works, also with `Where`, `OrderBy().Take()` and `Distinct()` (probed on MySQL 8.4). The library adds no `NO_MERGE` hint. Guard test: `Should_throw_error_1093_When_source_query_has_a_subquery_on_the_target_table`.
- **Date and time literals**: `ThinktectureMySqlQuerySqlGenerator.VisitSqlConstant` and the temp table defaults apply `MySqlTypeMappingExtensions.WithFractionalSecondsPrecision`, because the provider mapping of `HasColumnType("datetime(6)")` (also `timestamp(6)`, `time(6)`) has `Size` 6 and no `Precision`, and its literal would drop the fraction. Test: `Should_update_with_captured_datetime_with_fractional_seconds`. A `TimeSpan` constant (provider type `TimeSpan`) is written by `VisitSqlConstant` itself as `TIME '[-]HH:mm:ss.ffffff'` with the total hours (at least two digits) and always six fraction digits, because the provider literal uses `hh` and drops the days and the sign. MySQL rounds the fraction to the column precision, like for a parameter. `MySqlTimeFormatter` builds the text for the literal, for the default value of a temp table column and for the JSON of collection parameters. MySQL rejects a value outside `-838:59:59` to `838:59:59` with error 1525. A single-constant `Map` is a parameter and does not use the literal. Tests: `*time_span_beyond_one_day*`, `Should_throw_error_1525_for_captured_time_span_outside_the_range_of_TIME`.
- **Nested transactions**: MySqlConnector starts a transaction without a level with `SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ` and reports `IsolationLevel.Unspecified`. `NestedDbContextTransaction` treats `Unspecified` as `RepeatableRead`, so a child transaction with `ReadCommitted` or `RepeatableRead` is allowed under a root without level.
- **Temp tables (public API)**: connection-scoped `CREATE TEMPORARY TABLE`, removed with `DROP TEMPORARY TABLE IF EXISTS`. `MySqlTempTableCreator` writes the charset and the collation of the design-time relational model (`MySqlColumnCharSet`: annotations of the provider at property, complex member, entity type and model level; Guid `char(36)` with `ascii_general_ci`), like the migrations of the provider. `IDesignTimeModel` is requested only for a column type that can have a charset (`MySqlColumnCharSet.SupportsCharSet`: `char`, `varchar`, the `text` types, `enum`, `set`), and the table-level clauses are written only then. A temp table or a collection parameter without such a column needs no design-time model. The default is the `DefaultCharSet` of the provider, `utf8mb4` with its default collation `utf8mb4_0900_ai_ci`. For a hand-written schema in a database with another default collation, configure the model (`modelBuilder.UseCollation(...)` or entity/property level); otherwise joins and comparisons on string columns fail with error 1267. Nullability uses `IsColumnNullable(storeObject)`. A literal default of a TEXT/BLOB/JSON column is written as `DEFAULT (literal)` (error 1101 otherwise). A string default of every other column type is generated with `MySqlStringTypeMapping.Clone(replaceLineBreaksWithCharFunction: false)`, so a line break stays inside the quoted literal. Reason: the provider writes a line break as `CONCAT('a', CHAR(10), 'b')`, and MySQL accepts a function call as default only in parentheses (error 1064 otherwise). The clone is not applied to a TEXT/BLOB/JSON column, because the JSON mapping derives from `MySqlStringTypeMapping` and the clone would return a plain string mapping. Test: `Should_create_temp_table_with_string_default_value_with_line_break`. Date and time defaults use the provider-internal `IDefaultValueCompatibilityAware.Clone(isDefaultValueCompatible: true)` (EF1001), because MySQL rejects a `TIMESTAMP '...'` literal as default. A default whose provider type is `TimeSpan` is written by `MySqlTempTableCreator` itself as the quoted text `'[-]HH:mm:ss.ffffff'` of `MySqlTimeFormatter` (total hours, six fraction digits, no `TIME` prefix), because the provider literal uses `hh` and drops the days and the sign. MySQL rounds the fraction to the column precision. The provider type is read from the type mapping, so a `TimeSpan` that is stored as `bigint` keeps its ticks. A `TimeOnly` default uses the provider literal. Tests: `Should_create_temp_table_with_time_span_default_value_When_value_is_beyond_one_day_or_negative`, `Should_create_temp_table_with_ticks_default_value_When_time_span_is_converted_to_bigint`. `AUTO_INCREMENT` is emitted only for registered temp table entities and requires the column to be in the primary key. Owned entities throw `NotSupportedException`. `MySqlTempTableBulkInsertOptions.Advanced.UsePropertiesToInsertForTempTableCreation` (default `false`) creates the table with all columns; `true` creates it with the inserted columns only. MySQL cannot reference a temp table twice in one statement (error 1137: self-join, `EXISTS`, `UNION`). `MySqlTempTableCreator` throws `NotSupportedException` before the first statement when the leased name equals a table of the relational model that maps an entity type other than a temp table entity. The delimited identifiers are compared case-insensitively. Reason: a MySQL temporary table hides the base table with the same name for the connection, so queries and `SaveChanges` would use the temporary table, and its rows are lost with the drop. `DefaultTempTableNameProvider` returns the table name of the entity, so on MySQL it fits only temp table entities (`#Name`). Only tables of the relational model are checked, not views. The staging tables of update and upsert do not run this check. With `sql_require_primary_key=ON` a temp table without a primary key fails with error 3750: `BulkInsertValuesIntoTempTableAsync` and keyless entities with the default `PrimaryKeyCreation` (`EntityTypeConfiguration`). `IPrimaryKeyPropertiesProvider.AdaptiveForced` makes all columns the primary key. It needs NOT NULL columns (error 1171), no TEXT or BLOB column (error 1170, a `string` needs `HasMaxLength`) and distinct rows (error 1062). Otherwise use a temp table entity with a key. Tests: `*sql_require_primary_key_is_on`.
- **Collection parameters**: `JSON_TABLE(@json, '$[*]' COLUMNS (... PATH '$' ERROR ON ERROR))` with a `LIMIT` of the element count. The columns carry the charset and the collation of the model, like temp tables. `ERROR ON ERROR` raises error 3156, 3666 or 3669 for a value that does not fit the column type, instead of a NULL; JSON `null` stays NULL. `ERROR ON EMPTY` is not used, because a `JsonSerializerOptions` that omits null properties would fail. `decimal`, `TimeSpan`, `DateTime` and `DateTimeOffset` are serialized as strings that MySQL parses, because `JSON_TABLE` reads numbers as DOUBLE and rejects the .NET formats. A `DateTimeOffset` is written as its UTC value `yyyy-MM-dd HH:mm:ss.ffffff` without an offset, because `JSON_TABLE` converts a value with an offset into the session time zone and the provider stores UTC. `byte[]` is not supported: System.Text.Json writes Base64 text, which never equals a binary column. Paths of complex parameters honor `PropertyNamingPolicy` and `[JsonPropertyName]`. A `_bin` collation of the parameter against a `_ci` column compares case-sensitively and cannot use the index. Guid collections with `GuidFormat=Binary16` raise error 3669. `EF.Parameter(list)` does not work with Microting 10.0.14, also in plain EF Core queries: without `EnablePrimitiveCollectionsSupport()` it fails with "... Primitive collections support has not been enabled.", with it with "Expression '@ids' in the SQL tree does not have a type mapping assigned.".
- **Window functions**: `RowNumber` of the Relational package, `Sum`/`Average`/`Max`/`Min` in `MySqlWindowAggregateDbFunctionsExtensions` (1 to 5 partition columns, with or without ordering) and `NTile` in `MySqlDbFunctionsExtensions` (0 to 5 partition columns, ordering optional, returns `int`). `ROW_NUMBER()` is wrapped in `CAST(... AS signed)`, because MySQL returns BIGINT UNSIGNED. `MAX`/`MIN` over a Guid (`char(36)`) need no cast.
- **Bitwise aggregates**: `EF.Functions.BitOr`/`BitAnd`/`BitXor` (`MySqlAggregateDbFunctionsExtensions`, translator `MySqlBitwiseAggregateMethodCallTranslator`, always on) emit `BIT_OR`/`BIT_AND`/`BIT_XOR`, cast back to the type mapping of the input, inside a `CASE` that returns NULL for an empty group. Reason: `BIT_*` returns BIGINT UNSIGNED, and for an empty group `BIT_AND` returns all ones and `BIT_OR`/`BIT_XOR` return 0. `DISTINCT` is dropped for OR/AND and throws `NotSupportedException` for XOR.
- **Not available for MySQL**: a migrations generator with `IfExists()`/`IfNotExists()`, tenant database support, table hints, a `MySql.Testing` package and `AddRowNumberSupport`.
- **Not tested**: MariaDB, MySQL below 8.0.19, GTID with statement-based binary logging, and bulk copy into a table without insertable columns.

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
- SQL Server uses `IEntityDataReader.RowsRead`; PostgreSQL uses `NpgsqlBinaryImporter.CompleteAsync()` return; MySQL uses the sum of the `ExecuteNonQueryAsync()` results or `MySqlBulkCopyResult.RowsInserted`; SQLite uses `ExecuteNonQueryAsync()` return
- **Statement execution**: SQL text that the library builds (temp table DDL and primary key, `TRUNCATE`/`DELETE`, `MERGE`, `UPDATE ... FROM`, `INSERT ... ON CONFLICT`, the MySQL staging statements) is executed with `BulkOperationsSqlExecutor.ExecuteNonQueryAsync(ctx.Database, sql, cancellationToken)` (`Thinktecture.Internal`, BulkOperations package), not with `ExecuteSqlRawAsync`. Reason: `ExecuteSqlRawAsync` applies `String.Format` to the text, also without parameters, so a brace in a default value (`HasDefaultValue("{}")`, `HasDefaultValueSql("'{}'")`) or in an identifier throws `FormatException`, and `{{` is sent as `{`. The helper builds the command with `IRawSqlCommandBuilder.Build(sql)` and executes it as `ExecuteSqlRawAsync` does (concurrency detector, command logger, `CommandSource.ExecuteSqlRaw`), so interceptors, logging, the current transaction and the command timeout apply. `EfCoreValueExpressionTranslator.ExecuteNonQueryAsync` delegates to the overload with a command and parameter values. Temp table references drop their table with a plain `DbCommand`. `BulkOperationsSqlExecutor` has static methods and no extension methods, because IDEs such as ReSharper offer extension methods of namespaces that are not imported, so they would appear next to `ExecuteSqlRawAsync` on `DatabaseFacade` in the code of users. `[EditorBrowsable(EditorBrowsableState.Never)]` hides the class from the completion lists. The collection parameter factories (`SqlServerCollectionParameterFactory`, `NpgsqlCollectionParameterFactory`, `MySqlCollectionParameterFactory`) build their queries with `FromSqlRaw`, so their statement is a `String.Format` template with the placeholders `{0}` (row limit) and `{1}` (JSON parameter). Each factory doubles the braces of every fragment that it embeds (column names, column types with their charset and collation clauses, JSON property names and JSON paths) with its private method `EscapeBraces`. Reason: a brace in a column name (`HasColumnName("a{b}")`) otherwise throws `FormatException` when the query is executed, and a name that contains `{0}` or `{1}` gets the parameter placeholder in its place. `EscapeBraces` is a private method of each factory and not a shared helper, because a shared helper would have to be public. Tests: `*braces*` in the temp table creator, bulk update, upsert, truncate and collection parameter test classes.
- PostgreSQL resolves `NpgsqlDbType` per column via `INpgsqlTypeMapping` for correct type handling (e.g., `jsonb`/`json` from `string`)
- Table override via `TableNameProvider` (`IBulkOperationTableNameProvider.GetTableName(IEntityType)` returning `BulkOperationTableName(Name, Schema)`; `Schema == null` means database default, not model schema). Built-in: `IBulkOperationTableNameProvider.Default` (model tables; throws for temp table entities), `ForRootEntity(tableName, schema)` (root only; throws for separately mapped owned types), `ForRootEntityWithDefaultSchema(tableName)` (root only; fixed name, schema from the model via `entityType.GetSchema()`; throws for temp table entities; the obsolete `TableName` path does not), `ForRootEntityWithDefaultName(schema)` (root only; name from `Default`, so temp table entities throw; fixed schema, `null` emits no schema) and `Create(Func<IEntityType, BulkOperationTableName>)` (`DelegateTableNameProvider`; calls the delegate for the root and for every separately mapped owned type). Options `TableName`/`Schema` (also on the `*FromQueryOptions`) are `[Obsolete]`; their messages map `TableName` alone to `ForRootEntityWithDefaultSchema`, `Schema` alone to `ForRootEntityWithDefaultName` and both to `ForRootEntity`, and the `TableName` message names `ForRootEntity` for a temp table entity. Option classes delegate `TableName`, `Schema` and `TableNameProvider` to `Internal/BulkOperationTableNameOptions`: setting `TableName`/`Schema` replaces `TableNameProvider` with a root-only provider (`RootEntityTableNameProviderBase.FromObsoleteOptions`; a null part keeps the model value; it builds the same three root-only classes as the three `ForRootEntity*` factories, but without validation, so an empty part fails in the executor), setting `TableNameProvider` clears `TableName`/`Schema`, and the last assignment wins. `TableNameProvider` is non-nullable and defaults to `IBulkOperationTableNameProvider.Default`; setting it to `null` throws `ArgumentNullException`. The interface default member `TableNameProvider` builds the same provider for implementations that only implement `TableName`/`Schema`, or returns `Default` when both are null. Executors read `options.TableNameProvider` and never read `TableName`/`Schema`; the query-based operations take nullable options and read `options?.TableNameProvider ?? IBulkOperationTableNameProvider.Default`. Executors look up the entity type via `GetEntityTypeOrTempTableEntityType<T>()`, resolve the root table via `BulkOperationTableNameProviderExtensions.GetValidTableName(entityType)` and pass the provider down; the child loops call `tableNameProvider.GetValidTableName(childContext.EntityType)` when they reach an owned type. SQL Server, PostgreSQL and SQLite give no atomicity guarantee: when the provider throws for an owned type, rows of the root table may already be written. SQLite rolls them back only when the executor started its own transaction. Atomicity is the responsibility of the caller's transaction. The MySQL insert resolves and validates the tables of the root and of every separately mapped owned type before the first write, so an exception of the provider for an owned type writes no row. Temp table targets use `ForRootEntity(tempTable.Name)`; a user `TableNameProvider` is never applied to temp tables. Owned types in update/upsert: SQL Server/PostgreSQL reject every owned kind (temp table creation throws `NotSupportedException` "Temp tables don't support owned entities." before the target is written); MySQL rejects every owned kind before the first statement (see "MySQL specifics"); SQLite update writes inlined, separate `OwnsOne` and `OwnsMany` via the child loop (root table needs a non-key column, otherwise empty `SET` and syntax error); SQLite upsert supports inlined only; the `BulkInsertOrUpdateContext` constructor throws `NotSupportedException` for separate owned types (no update properties, because update properties are inlined-only) before the first write. Tests: `Should_*owned_type*`/`Should_*owned_collection*` in the `BulkUpdateAsync`/`BulkInsertOrUpdateAsync` test classes of each provider. Property selection via `IEntityPropertiesProvider`.
- PostgreSQL temp table collation: strings with `.` split into schema+name by default; set `SplitCollationComponents = false` to escape as single identifier
- Temp table collation (SQL Server, PostgreSQL) is read from the design-time model (`IDesignTimeModel`), because the runtime model has no collation. `GetDesignTimeProperty` in `SqlServerTempTableCreator`/`NpgsqlTempTableCreator` finds a string member of a complex type through its `IComplexType.ComplexProperty` chain; `GetEntityType(property.DeclaringType.Name)` alone (the library extension `RelationalModelExtensions.GetEntityType(IModel, string)`) throws `Thinktecture.EntityFrameworkCore.EntityTypeNotFoundException` for complex types. SQLite has no collation lookup.
- **Complex types** (`ComplexProperty`, class or struct, nested allowed): `BulkInsertAsync`, temp tables (DDL and `tempTable.Query`) and the COPY/`SqlBulkCopy` paths use `GetFlattenedProperties()` and the configured column names; the member getter, the column nullability and the discriminator are described in the "Nullable complex properties" bullet. Query-based bulk update/insert accept a target selector through complex properties (`.Set(e => e.Boundary.Upper, (e, f) => f.Boundary.Upper)`, `.Map(e => e.Boundary.Upper, f => f.Boundary.Upper)`) and a whole complex property (`.Set(e => e.Boundary, (e, f) => f.Boundary)`, also from a captured value or a `new` expression without a lambda parameter, which is evaluated on the client). `BulkUpdateFromQueryHelper.ExtractPropertyAccess` records the complex members in `PropertyAccessInfo.ComplexPath`, `ResolveProperty` walks it via `FindComplexProperty`, and `ExpandComplexProperties` (called once per operation by `EfCoreValueExpressionTranslator`) returns one `ResolvedSetPropertyEntry` (resolved target `IProperty`, the caller's target selector for error messages only, value selector) per target column. It replaces a whole-complex entry with one entry per member, recursively, by appending the member access to the value selector, so a generated selector is never extracted again. A conditional (`?:`) or a null-coalescing (`??`, rewritten to `a == null ? b : a`) value over complex values is split per branch by `BuildPerBranch`, recursively, so every leaf is a source or target column, a client value with its null guard, or a discriminator constant (`EF.Property` for a column branch). Branch types are lifted to the nullable type when they differ. A coalesce with a column as left operand (`s.Opt ?? e.Opt`) becomes one CASE per member column whose test is the null test of EF Core for the complex value (a required member column or the discriminator `IS NULL`). Members with a value converter work in all these paths; a captured value is emitted with the converter of the target column. `ExpandComplexProperties` skips generated members (`GetBeforeSaveBehavior() == Ignore`, e.g. computed columns), the same rule as `IEntityPropertiesProvider`; an explicit `Set`/`Map` of such a member still writes it, and a whole complex property whose writable members are all generated throws `ArgumentException` ("Expression: <that selector>", checked per entry, also when other `Set`/`Map` entries exist). Duplicate target columns (also a whole complex property plus one of its members) throw `ArgumentException` on all providers, checked in both translator loops with a `HashSet<IProperty>`. A captured `null` that is dereferenced during client evaluation (the whole value of a required complex property, also a null captured `TimestampPair?` for a required struct complex property, or a member access on a null captured object, also in filters) throws `ArgumentException` from `ClosureFlatteningVisitor`; a null guard (`captured == null ? 0 : captured.Count`, `nullableInt.HasValue ? 1 : 2`, also `captured == null || e.Name == captured.Name`) prevents the exception, see the "Value expression evaluation" bullet. `EF.Property` must be called on the lambda parameter (`NotSupportedException`). `ResolveProperty` names the declaring type (the complex type for a complex member) in its `InvalidOperationException`. Not supported: complex collections (`NotSupportedException`) and a member through a nullable complex property, class or struct (`e.OptionalBoundary!.Upper`, `e.EffectiveMessage!.Value.ReceivedAt`, `NotSupportedException` "The member '…' of '…' is a nullable complex property."; checked in `ResolveDeclaringType` before the next path element, so it covers `Set`, `Map` and nested paths). Reason: EF Core writes only that column, and when the complex property of a row is null, EF reads the row back as null or reads the other required members as their default value, so the written value is lost without an error. EF Core cannot bind a nested complex type to a constructor parameter, so nested complex types need settable or init members. Tests: `*ComplexType*` and `*value_converted*` in the PostgreSQL bulk test classes, `Should_update_*ComplexType*` in the SQL Server and SQLite `BulkUpdateFromQueryAsync` classes.
- **Nullable complex properties** (optional complex types, class or struct, nested in both directions): the semantics are those of `SaveChanges`. A null container writes NULL into every member column, a non-null container writes the member values, and the discriminator of `HasDiscriminator()` is written. The only shadow property of a complex type is the discriminator of `HasDiscriminator()`; every path writes `complexType.GetDiscriminatorValue()` for a non-null container and NULL for a null container, because EF Core decides null-ness by the discriminator column when one exists. Any other shadow member throws `NotSupportedException` in the query-based expansion. Entity-based paths (`BulkInsertAsync`, `BulkUpdateAsync`, `BulkInsertOrUpdateAsync`, `BulkInsertIntoTempTableAsync`) read the container of a member level by level in `PropertyGetterCache.BuildComplexPropertyGetter` (`GetGetter().GetClrValue` of each complex property, applied to the value of the level above) and stop at the first null level, so a null container anywhere along the chain yields NULL in every member column and NULL for the discriminator, also for a struct complex type (required or with `HasDiscriminator()`) inside a null class container. EF Core's chain getter `GetClrValueUsingContainingEntity` is not used for complex members, because it returns `default(T)` for a struct complex type when an outer container is null. This rule also applies to a required class complex property that is null on the CLR object; the database rejects it for NOT NULL columns, and `SaveChanges` throws in this case as well. Temp table DDL uses `property.IsColumnNullable(storeObject)`, so the member columns of an optional complex type are NULL columns, and `tempTable.Query` reads a null row back as null. The query-based `Set`/`Map` of a whole nullable complex property copies source columns as they are (no CASE, a partially filled source row is copied unchanged). A client-evaluated value is wrapped per member in `value == null ? null : value.Member` (`.Value.Member` for a struct), with one guard per chain level and only when the chain contains a nullable complex property; `ClosureFlatteningVisitor` then emits NULL literals or the member values. A value is client-evaluated (`BulkUpdateFromQueryHelper.IsClientEvaluated`) when every `ParameterExpression` in it is bound by a lambda inside the value, so `boundaries.FirstOrDefault(b => b.Upper == 5)` gets the null guard and the discriminator constant; a value with the lambda parameters `e`/`s` is a server value (`EF.Property` for the discriminator, plain member access). A conditional or coalesce value (`captured == null ? s.Opt : captured`, `captured ?? s.Opt`, `s.X > 0 ? s.Opt : e.Opt`) is split per member column. A client-evaluated test selects the branch in `ClosureFlatteningVisitor` (no CASE). A server test becomes one `CASE` per member column, also for the discriminator. `EfCoreValueExpressionTranslator` creates the column expressions with `property.IsColumnNullable()`. `BulkInsertValuesIntoTempTableAsync` with a complex-typed column is not covered. Tests: `*nullable_ComplexTypes*` and `*struct_members_of_null_ComplexType*` (entity `TestEntityWithStructsInOptionalComplexType`, raw-SQL assertion of the member columns) in the bulk and temp table test classes of all providers.
- **Query-based bulk update** (all providers): `DbSet<TTarget>.BulkUpdateAsync(sourceQuery, targetKey, sourceKey, setBuilder, filter?, options?)`. SQL Server generates `UPDATE t SET ... FROM target INNER JOIN (sourceQuerySql) AS s ON ... [WHERE filterCondition]`; PostgreSQL/SQLite generate `UPDATE target AS t SET ... FROM (sourceQuerySql) AS s WHERE ... [AND filterCondition]`; MySQL generates `UPDATE target AS t INNER JOIN <source table or (sourceQuerySql)> AS s ON ... SET ... [WHERE filterCondition]`. Uses `SetPropertyBuilder<TTarget, TSource>` for fluent property assignment. Key selectors support single and composite keys (anonymous types). Parameters are extracted by `QueryCompiler.ExtractParameters` in `CompileQuery`. Optional `filter` (`Expression<Func<TTarget, TSource, bool>>`) restricts which rows are updated; filter can reference both target and source properties (e.g., `(e, f) => e.Count < f.Count`); filter is translated by the same SQL translator as the SET values; a filter that translates to a constant `false` (e.g. `captured != null && ...` with a null `captured`, or EF folding `col > 0 && false`) is applied as `1 = 0`, because SQL Server rejects a bare `CAST(0 AS bit)` as a condition; a constant `true` is dropped by `ApplyPredicate`. Shared expression analysis logic in `BulkUpdateFromQueryHelper`. Table override via `TableNameProvider` on `SqlServerBulkUpdateFromQueryOptions`, `NpgsqlBulkUpdateFromQueryOptions`, `MySqlBulkUpdateFromQueryOptions`, or `SqliteBulkUpdateFromQueryOptions` (all implement `IBulkOperationFromQueryOptions`; obsolete `TableName`/`Schema` replace `TableNameProvider` through the same `BulkOperationTableNameOptions`; `EfCoreValueExpressionTranslator` receives the provider and applies it to the target table only: before `Postprocess` it replaces `Tables[0]` of the join `SelectExpression` and the `Table` of the `UpdateExpression` with a `TableExpression` whose `ITableBase` is `Internal/BulkOperationTargetTable`, a decorator of the model table that returns the name and schema of the provider; a `TableExpression` takes its name and schema from its `ITableBase`, and `WithAlias`/`Clone` rebuild it from that `ITableBase`, so alias postprocessing keeps the override; the source query keeps its own `TableExpression` instances, so a source query over the model table of the target still reads the model table; the setter columns, the filter and the value expressions reference the target by alias, so a target member reads the override row; the SQL text is not edited, and the `ITenantDatabaseProvider` of SQL Server receives the override name and schema for the UPDATE target). Value expressions support constants, captured variables, and static members (e.g., `.Set(e => e.Count, (e, f) => 42)`, `.Set(e => e.Name, (e, f) => myVar)`, or `.Set(e => e.Status, (e, f) => Status.Published)`), complex arithmetic expressions referencing target and/or source properties (e.g., `.Set(e => e.Count, (e, f) => e.Count + f.Count * 2)` or `.Set(e => e.Count, (e, f) => f.Count + 10)`), in addition to simple property access and `EF.Property` calls. Captured values and static members are evaluated to constants and rendered as SQL literals (`SqlConstantExpression` with the target column's type mapping, so a value converter is applied); other expressions are translated by EF Core's `RelationalSqlTranslatingExpressionVisitor` through `TranslateProjection(flattened, applyDefaultTypeMapping: false)`, and `TranslateValueExpression` applies the type mapping of the target column to the result. Reason: `SqlExpressionFactory.ApplyTypeMapping` keeps an existing mapping, so with the default mapping a `CASE` whose branches are all client values (`s.Count > 15 ? Status.C : Status.B`, also with a captured variable, an `ids.Contains(s.Id)` test, in the insert path and for every member of a whole complex property) keeps the default mapping of the CLR type and ignores the converter (an enum with `HasConversion<string>()` stores `2` instead of `'C'`, an `int` column with `HasConversion(v => v * 100, v => v / 100)` stores `2` instead of `200`), and a type without default mapping (`ConvertibleClass`) fails with "Could not translate value expression". A `CASE` with a column branch inherits the column mapping. Arithmetic over such a `CASE` gets the mapping of the binary expression, as in EF Core's `ExecuteUpdate`. A conditional inside the source projection (`Select(x => new { V = x.Count > 15 ? Status.C : Status.B })` mapped with `s => s.V`) is translated as part of the source query with EF Core's inferred mapping, so the target converter does not apply to it. The filter keeps the default mapping, because it has no target column. The only SQL parameters of the UPDATE come from the source query.
- **Query-based bulk insert** (all providers): `DbSet<TTarget>.BulkInsertAsync(sourceQuery, mapBuilder)`. Generates `INSERT INTO target (cols) SELECT s.cols FROM (sourceQuerySql) AS s` — identical structure across all four providers (only identifier quoting differs). Uses `InsertPropertyBuilder<TTarget, TSource>` for fluent column mapping via `Map(target => target.Prop, source => source.Prop)` with single-parameter value selector. No key selectors needed (insert, not join). Table override via `TableNameProvider` on `SqlServerBulkInsertFromQueryOptions`, `NpgsqlBulkInsertFromQueryOptions`, `MySqlBulkInsertFromQueryOptions`, or `SqliteBulkInsertFromQueryOptions` (`IBulkOperationFromQueryOptions`, same as bulk update from query). Value selectors support constants, captured variables, and static members (e.g., `.Map(e => e.Count, f => 42)` or `.Map(e => e.Name, f => myVar)`), complex arithmetic expressions referencing source properties (e.g., `.Map(e => e.Count, f => f.Count * 2 + 1)`), in addition to simple property access and `EF.Property` calls. A value selector that is a single constant after client evaluation (a constant, a captured variable, a static member, `captured == null ? 0 : captured.Count`, a `new` expression) is bound as a parameter `__insert_const_N` (the type mapping of the parameter applies the value converter when the parameter is bound, and the SQL text does not depend on the value); a `null` value becomes a `NULL` literal through the parameter-based SQL processor; constants inside a larger expression are rendered as SQL literals; other expressions are translated by EF Core's `RelationalSqlTranslatingExpressionVisitor`. The same source column or expression can be mapped to several target columns: `SelectExpression.ApplyProjection` merges equal value expressions into one projection (`AddToProjection` compares by `Equals`), therefore `TranslateInsertFromQuery` looks up the merged `ProjectionExpression` of every target column and builds the SELECT for rendering with the public `SelectExpression.Update`, which repeats a merged projection per column. Without this step the statement fails with "N values for M columns" (SQLite), Msg 120 (SQL Server) or 42601 (PostgreSQL). A source whose `SelectExpression` has `IsDistinct` (`Select(...).Distinct()`, `Set<T>().Distinct()`, also followed by `Where`) is pushed into a subquery with `selectExpression.PushdownIntoSubquery()` before the values are translated, the rule of EF Core's `TranslateSelect` after `Distinct`, so the DISTINCT key stays the projection of the source and the value expressions reference the outer columns (`SELECT "t0"."Count", @__insert_const_0 FROM (SELECT DISTINCT "t"."Name", "t"."Count" FROM ...) AS "t0"`). Without the pushdown, `ReplaceProjection` made the mapped values the DISTINCT key: a mapping of a subset of the columns lost rows, and a non-deterministic value (`EF.Functions.Random()`) produced more rows than the source has. Only `Distinct().OrderBy().Take()` is pushed down by EF Core alone. In `Take().Select().Distinct()` EF Core pushes down only the `Take`, and the `Select` keeps `IsDistinct`, so the library pushdown applies.

- **Value expression evaluation** (query-based bulk update/insert): `FlattenClosures` in `EfCoreValueExpressionTranslator` runs a rewrite and two passes. `SpanConversionRewritingVisitor` rewrites `MemoryExtensions.Contains` and `MemoryExtensions.SequenceEqual` whose span arguments are the implicit conversion `op_Implicit(array)` of `Span<T>`/`ReadOnlySpan<T>` to the `Enumerable` methods with the same generic argument (`Expression.Call(typeof(Enumerable), name, ...)`), the rule of EF Core's `ExpressionTreeFuncletizer` for a query; a null comparer argument is dropped, a non-null comparer is passed to the three-argument `Enumerable.Contains`, which a server value fails to translate ("Could not translate filter expression" or "Could not translate value expression"). Reason: C# 14 binds `ids.Contains(s.Id)` for an `int[]`, `Guid[]` or `string[]` (also `new[] { 1, 2 }.Contains(s.Id)`) and `a.SequenceEqual(b)` for two arrays to the span methods, and a span is a ref struct, so the client evaluation (`Compile(preferInterpretation: true)`) of every subtree that contains it, also a fully client-evaluatable one such as `items.Where(i => ids.Contains(i.Id)).Count()`, fails with "violates the constraint of type 'TRet'"; the SQL translator has no translation for `MemoryExtensions` either. No other span shape reaches the library: the compiler rejects every other ref struct value in an expression tree (CS8640), and `Max`/`Min`/`Count`/`Any`/`First`/`Sum` on an array bind to `Enumerable`. After the rewrite an array behaves like a `List<T>` (`IN (...)`, `=` for one element, a false condition for an empty array). `EvaluatableSubtreeFinder` marks every node whose subtree contains no extension node (the entity shapers that replace the outer lambda parameters), whose nodes all pass `IEvaluatableExpressionFilter`, so server-translatable members (e.g. `DateTime.Now`) stay untouched, and whose `ParameterExpression`s all belong to lambdas inside the subtree. It keeps the parameters of the lambdas in scope with their nesting depth, so `items.Select(i => i.Id).Contains(s.Id)`, `items.Where(i => i.Active).Count()` and nested lambdas such as `items.Where(i => items.Any(j => j.Id == i.Id))` are evaluated on the client (the rule of EF Core's funcletizer for `Enumerable.Select`, applied to every method), while `items.Where(i => i.Id == s.Id)` is not, because `s` is a shaper. A subtree that uses a parameter of an enclosing lambda (`i.Id` inside that `Where`) is never an evaluation root, because the parameter is unbound outside its lambda; a `LambdaExpression` or a `Quote` node is never a root either, so the lambda of a server-side call stays a lambda. The evaluated collection is a `ConstantExpression` (also a deferred `IEnumerable<T>`), which EF Core's relational `ContainsTranslator` renders as inline `IN (...)` on every provider; a collection with one element becomes `=`, and an empty collection becomes a false predicate. `ClosureFlatteningVisitor.VisitMethodCall` materializes the constant collection of a two-argument `Contains` once (static `Enumerable.Contains(collection, item)` or an instance `Contains(item)` of a collection type: `IList`, `ICollection<T>`, `IReadOnlySet<T>` or `IImmutableSet<T>`, the rule of EF Core's internal `IsContainsMethod`, copied in `IsCollectionType`) and rebuilds the call as `Enumerable.Contains(nonNullElements, item)` over an array, because EF Core would enumerate a single-use or lazy sequence a second time. The materialization holds per `Contains` call, not per operation: a single-use sequence that is used in two `Set`/`Map` entries, or in a whole complex value that is expanded per member, is empty the second time. The `null` elements are removed, because `ContainsTranslator` calls `SqlExpressionFactory.Constant(element)` and `SqlConstantExpression` throws `NullReferenceException` for `null`. When a `null` element was present, the call gets `|| item == null`, except when `UseRelationalNulls` is set, which `FlattenClosures` reads from `RelationalOptionsExtension`. An empty array renders a false predicate, so a collection with only `null` elements renders `item IS NULL`. A `null` collection becomes `Constant(false)`, because `ContainsTranslator` has no translation for it. Both match EF Core's SQL for a collection parameter (`col IN (...) OR col IS NULL`, `col IN (...)` with relational nulls, `WHERE 0`): the filter path renders the false constant as `1 = 0`, and inside a value EF Core folds the `CASE`. Not rewritten: a `Contains` with a comparer, `string.Contains`, and the `Contains` of a non-collection object (a captured `NpgsqlRange<T>` keeps Npgsql's range translation, an object with its own `Contains` keeps "Could not translate filter expression"); a `null` collection in a client-evaluated subtree (`ids!.Contains(5)`) throws the `InvalidOperationException` of the client evaluation (`ArgumentNullException` of `Enumerable.Contains`), as in EF Core's funcletizer. `EvaluatableSubtreeFinder` never marks a method call whose first parameter is `DbFunctions` (`EF.Functions.RowNumber(EF.Functions.OrderBy(1))` has no client implementation) or a node whose type is assignable to `IQueryable` (a query captured with the static type `IQueryable` is not executed during translation, and such a value selector fails with "Could not translate value expression"; a query captured as `IEnumerable<T>` runs synchronously during translation, without the cancellation token, as a snapshot). `ClosureFlatteningVisitor` replaces each topmost marked node (except a `ConstantExpression`) with a `ConstantExpression` by evaluating it once via `Expression.Lambda<Func<object?>>(Expression.Convert(node, typeof(object))).Compile(preferInterpretation: true)`. One evaluation holds per subtree of one target column: `ExpandComplexProperties` expands a whole complex value to one entry per member, so a client-evaluated complex value is evaluated once per member column and once more per null guard, and a stateful method can give the member columns values from different calls. A subtree is evaluated as a whole, so closure fields, static members (e.g. smart-enum values like `Status.Published`), `nullableInt.HasValue ? 1 : 2`, `captured == null ? 0 : captured.Count` and `captured.Name.ToUpper()` follow C# semantics and become literals. Like EF Core's funcletizer, `ClosureFlatteningVisitor` short-circuits: a `ConditionalExpression` whose `Test` is marked is replaced by the visited selected branch (converted to the conditional's type if needed), and an `AndAlso`/`OrElse` without a custom `Method` whose `Left` is marked becomes `Constant(false)`/`Constant(true)` or the visited `Right` (a lifted `bool?` left operand that is `null` keeps the operator). So `captured == null || e.Name == captured.Name` and `captured == null ? f.Count : captured.Count + f.Count` work for a null `captured`. When the evaluation throws a `NullReferenceException`, or an `InvalidOperationException` (then only `Nullable<T>.Value` accesses are checked), `FindMemberOfNullInstance` looks for the innermost member access whose instance is `null`; if it finds one, the result is `ArgumentException` "The member '…' of '…' cannot be read because the captured value is null. Provide a non-null value. Expression: …" (also for `n.Value` on a null `int?`, which throws `InvalidOperationException` internally). Any other `NullReferenceException` becomes `ArgumentException` "The expression cannot be evaluated because a captured value is null. Expression: …", and every other exception becomes `InvalidOperationException` "The expression '…' cannot be evaluated on the client: …"; both keep the original exception as inner exception. Resulting `ConstantExpression`s bypass EF's SQL translator (which fails for types without a built-in type mapping) and are emitted directly with the target column's type mapping, so value-converter-typed values work; in the insert path, the `TypeMappedRelationalParameter` of the rendered command applies the converter when the parameter is bound. `ClosureFlatteningVisitor` does not re-evaluate a member access whose instance became a constant through short-circuiting. Therefore a user-written member access on a conditional over complex values as the value of a single member target (`.Set(e => e.Boundary.Lower, (e, s) => (captured == null ? s.Boundary : captured).Lower)`) fails with `InvalidOperationException` "Could not translate value expression" when the client branch is selected. Write the conditional per member, or set the whole complex property. Scalars are not affected, because EF Core translates a member of a scalar constant. Before SQL generation, both paths run the parameter-based SQL processor of the provider (`ProcessParameters` in `EfCoreValueExpressionTranslator`: `IRelationalParameterBasedSqlProcessorFactory` with `UseRelationalNulls` and `ParameterizedCollectionMode` from `RelationalOptionsExtension`). The update path runs it on the `UpdateExpression`, the insert path on the projected `SelectExpression`. Therefore a null comparison in the source query, in `filter` or in a value expression renders `IS NULL`, a parameter with a null value becomes a `NULL` literal, a collection parameter is expanded into one parameter per element or sent as one JSON parameter, and the search-condition conversion of SQL Server runs. A comparison between two nullable columns treats two NULL values as equal unless `UseRelationalNulls` is set. The join predicate keeps a plain `=` for a single key, so a NULL key never matches; a composite key gets the compensation of EF Core, so two NULL parts match unless `UseRelationalNulls` is set. Before `ProcessParameters`, both paths run EF Core's query postprocessor (`Postprocess`: `IQueryTranslationPostprocessorFactory.Create(compilationContext).Process` on the `UpdateExpression` or on the projected `SelectExpression`), which is EF Core's order for `ExecuteUpdate`: type mapping inference, pruning, alias postprocessing, simplification, value-converter compensation and provider passes such as the OPENJSON parameter mapping of SQL Server. Without it, `EF.Parameter(list)` and `ParameterTranslationMode.Parameter` fail on SQL Server with a `NullReferenceException` in `TypeMappedRelationalParameter.AddDbParameter`. The processor writes expanded parameters into the same dictionary. The insert path adds the `__insert_const_N` parameters before the processor runs, because `ParametersCacheDecorator.IsNull` throws for an unknown name. The translator returns an `IRelationalCommand` built by `CreateCommand` (`IRelationalCommandBuilderFactory`: the SQL text, which the insert path prefixes with `INSERT INTO ... (cols)`, plus the `IRelationalParameter`s of the rendered command, which carry the type mappings) together with the parameter dictionary. The executors call `EfCoreValueExpressionTranslator.ExecuteNonQueryAsync`, which does what `ExecuteSqlRawAsync` does (concurrency detector when thread safety checks are enabled, `RelationalCommandParameterObject` with the relational connection, the context and the command logger, `CommandSource.ExecuteSqlRaw`), so interceptors, logging, the current transaction and the command timeout apply. Dictionary entries that the SQL does not reference are ignored by `RelationalParameterBase.AddDbParameter`. Therefore a captured value of a property with a value converter, a `FromSqlRaw`/`FromSqlInterpolated` argument (`CompositeRelationalParameter`, value `object[]`) and a JSON collection parameter are bound correctly. Tests: `*value_converted*`, `*FromSql_with_argument*`, `*collection_parameter*`, and `*parameterized_collection_mode*` in the `BulkUpdateFromQueryAsync`/`BulkInsertFromQueryAsync` classes, and `*table_hints_and_window_function*` (library expressions through the postprocessor) in the SQL Server `BulkUpdateFromQueryAsync` class.

- **Source entity type resolution** (query-based bulk update/insert): Source entity type is resolved via `FindEntityType(typeof(TSource))` first (regular CLR-type lookup), then falls back to `FindEntityType(EntityNameProvider.GetTempTableName(typeof(TSource)))` for temp-table-only entities registered via `ConfigureTempTableEntity<T>()`. Without the fallback, column name resolution uses property names instead of mapped column names, causing SQL errors.

### Extension Architecture

**Four-tier extension hierarchy:**
```
DbContextOptionsExtensionBase (shared utilities)
  ├─ RelationalDbContextOptionsExtension (provider-agnostic)
  ├─ SqlServerDbContextOptionsExtension
  ├─ NpgsqlDbContextOptionsExtension
  ├─ MySqlDbContextOptionsExtension
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
  ├─ ThinktectureNpgsqlQueryableMethodTranslatingExpressionVisitor
  └─ ThinktectureMySqlQueryableMethodTranslatingExpressionVisitor

EF.Functions.* → IMethodCallTranslatorPlugin
  ├─ RelationalMethodCallTranslatorPlugin → RelationalDbFunctionsTranslator
  ├─ NpgsqlMethodCallTranslatorPlugin → NpgsqlDbFunctionsTranslator
  └─ MySqlMethodCallTranslatorPlugin → MySqlDbFunctionsTranslator

EF.Functions.Bit* (GROUP BY) → IAggregateMethodCallTranslatorPlugin
  ├─ NpgsqlAggregateMethodCallTranslatorPlugin
  └─ MySqlAggregateMethodCallTranslatorPlugin → MySqlBitwiseAggregateMethodCallTranslator

SQL Generation → IQuerySqlGeneratorFactory
  ├─ ThinktectureSqlServerQuerySqlGeneratorFactory
  ├─ ThinktectureNpgsqlQuerySqlGeneratorFactory
  └─ ThinktectureMySqlQuerySqlGeneratorFactory

Parameter-based SQL processing → IRelationalParameterBasedSqlProcessorFactory
  └─ ThinktectureMySqlParameterBasedSqlProcessorFactory
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
| `GetClrValueUsingContainingEntity` for a member of a complex type, or for the container of a nested complex type | A null container yields default values (e.g. 0) instead of NULL; a struct complex type inside a null outer container yields its default values and its discriminator value | Read the chain level by level with `GetGetter().GetClrValue` and stop at the first null (`PropertyGetterCache.BuildComplexPropertyGetter`) |
| `property.IsNullable` in temp table DDL | Members of an optional complex type get NOT NULL columns | Use `property.IsColumnNullable(storeObject)` |
| Design-time lookup by `property.DeclaringType.Name` | `Thinktecture.EntityFrameworkCore.EntityTypeNotFoundException` for a string member of a complex type in a temp table | Walk `IComplexType.ComplexProperty` up to the entity type (`GetDesignTimeProperty`) |
| Member access on a conditional over complex values as the value of a single member target | `InvalidOperationException` "Could not translate value expression" when the captured branch is selected | Move the member access into the branches, or set the whole complex property (`ExpandComplexProperties` splits the conditional) |
| `GetCommand` on an `UpdateExpression`/`SelectExpression` without `ProcessParameters` | `col = NULL` in the SQL, 0 affected rows, no error | Run `ProcessParameters` first, after every raw parameter is in the dictionary, then build the command with `CreateCommand` and execute it with `ExecuteNonQueryAsync` |
| Binding the translator's parameter values with `new SqlParameter(name, value)` or `ExecuteSqlRawAsync` | "No mapping exists from object type ..." for a value-converted value, a JSON collection (`List<T>`) or the `object[]` of a `FromSql` argument | Execute the `IRelationalCommand` of the translator through `EfCoreValueExpressionTranslator.ExecuteNonQueryAsync` |
| `GetCommand` without `Postprocess` | `NullReferenceException` in `TypeMappedRelationalParameter.AddDbParameter` for `EF.Parameter(list)` on SQL Server | Run `Postprocess` before `ProcessParameters` |
| `ReplaceProjection` + `ApplyProjection` as the final projection of an `INSERT ... SELECT` | "3 values for 5 columns" (SQLite), Msg 120 (SQL Server), 42601 (PostgreSQL) when two target columns use an equal value expression | Rebuild the projection list per target column with `SelectExpression.Update` after `ApplyProjection` |
| Returning the raw `TValue` from an `IEntityDataReader` (`ValueDataReader.GetValue`) | A struct with a converter fails with "No mapping exists from object type ..." (SQLite), a `SqlBulkCopy` conversion error or an Npgsql `InvalidCastException`; an enum with `HasConversion<string>()` stores the number without an error on SQLite | Apply `property.GetValueConverter() ?? property.GetTypeMapping().Converter` to non-null values, the rule of `PropertyGetterCache.BuildPropertyGetter` |
| Applying the `TableNameProvider` of a query-based update to the UPDATE text with `string.Replace` | Every reference to the model table is replaced, also inside the source subquery, so a source over the model table of the target reads the override table (wrong values, or a missing column error) | Replace the target `TableExpression` in the tree before `GetCommand` (`BulkOperationTargetTable` as the `ITableBase` of a new `TableExpression`, in `Tables[0]` of the select and in the `UpdateExpression`) |
| Evaluating a subtree that references a parameter of an enclosing lambda (e.g. `i.Id` in `items.Where(i => i.Id == s.Id)`) | `InvalidOperationException` "cannot be evaluated on the client: unbound variable: i" | Only a subtree whose lambda parameters are all bound inside it is an evaluation root (`_childrenMinParameterDepth` in `EvaluatableSubtreeFinder`) |
| Client evaluation of a subtree that contains the span conversion `op_Implicit(array)` (C# 14 binding of `array.Contains(x)` and `a.SequenceEqual(b)` to `MemoryExtensions`) | `InvalidOperationException` ``"cannot be evaluated on the client: GenericArguments[1], 'System.ReadOnlySpan`1[...]' ... violates the constraint of type 'TRet'"`` | Rewrite the call to the `Enumerable` method before the finder (`SpanConversionRewritingVisitor` in `FlattenClosures`); a ref struct guard in the finder is not enough, because the root of a client-only subtree has a scalar type |
| `TranslateProjection(expression)` with the default type mapping before `ApplyTypeMapping(targetMapping)` | A `CASE` over client values ignores the value converter of the target column (silent wrong data), or "Could not translate value expression" for a type without default mapping | Translate with `applyDefaultTypeMapping: false`, then apply the target mapping |
| `ReplaceProjection` on a source `SelectExpression` with `IsDistinct` | The mapped values become the DISTINCT key: fewer rows for a subset of the columns, more rows for a non-deterministic value such as `EF.Functions.Random()` | `PushdownIntoSubquery()` before the values are translated when `IsDistinct` is true |
| `ContainsTranslator` with a constant collection that has a `null` element, or with a `null` constant collection | Raw `NullReferenceException` from `SqlConstantExpression`, or "Could not translate filter expression" | Rewrite in `ClosureFlatteningVisitor.VisitMethodCall`: `Contains(nonNulls, item) \|\| item == null` (without the null test under `UseRelationalNulls`), `Constant(false)` for a `null` collection; only for `Enumerable.Contains` and a collection type (`IsCollectionType`) |
| A MySQL bulk operation that falls back to row-by-row statements or to `DELETE` | Different semantics and performance without a sign to the caller | Throw instead; every MySQL bulk operation uses exactly one strategy |
| MySQL table with a schema under `SchemaBehavior` `Throw` | `DelimitIdentifier` drops the schema, so the statement addresses the current database | Throw `NotSupportedException` before the first statement (`EnsureSchemaIsSupported`); users set `SchemaBehavior` `Ignore` or `Translate` |
| `CREATE TEMPORARY TABLE` inside a MySQL XA transaction | Error 4091 (`xa_detach_on_prepare=ON`, the default) | Convert the error of the staging `CREATE` into `NotSupportedException`; users disable XA, use a DbContext transaction, or set `xa_detach_on_prepare=OFF` |
| A MySQL statement that references a temp table twice (`INSERT ... WHERE NOT EXISTS`, self-join, `UNION`) | Error 1137 "Can't reopen table" | Use separate statements; the upsert deletes the matched rows from the staging table before the `INSERT ... SELECT` |
| MySQL staging table without a primary key | Error 3750 with `sql_require_primary_key=ON`, which applies to temporary tables as well | `__ord` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY in every staging table |
| Index on a TEXT/BLOB/JSON key column of a MySQL staging table | Error 1170 | No key index for such keys; duplicates are not detected |
| Key index on an AUTO_INCREMENT key in a MySQL upsert | Several new rows with id 0 collide with error 1062 | No key index when an upsert key column is store-generated (`IdentityColumn` and `IsCompatibleAutoIncrementColumn`) |
| MySQL key wider than 3,072 bytes | Error 1071 from the `UNIQUE (keys)` index of the staging table | No code handling; documented and tested |
| `INSERT ... SELECT` from the MySQL staging table without `ORDER BY __ord` | Error 1452 for a child row that sorts before its parent, because InnoDB checks foreign keys row by row | `ORDER BY s.__ord` |
| Substring test on a MySQL column type (`Contains("text")`) | `enum('text')` is treated as a TEXT column | Compare the base type (`MySqlTempTableCreator.GetBaseType`, `IsTextBlobOrJson`) |
| `MySqlBulkCopy.WriteToServerAsync` with the `IEntityDataReader` itself | `NotSupportedException` from `EntityDataReader.GetValues` | Pass the adapter `MySqlBulkCopyDataReader` |
| An exception that escapes from `IDataReader.Read` or `GetValues` during `MySqlBulkCopy.WriteToServerAsync` | `MySqlException` "Error during LOAD DATA LOCAL INFILE"; the connection is closed, the transaction of the caller is rolled back, and later statements run in autocommit mode without an error | Read the row and its values in `MySqlBulkCopyDataReader.Read`, store the exception, return `false`, and rethrow it after `WriteToServerAsync` |
| `GenerateSqlLiteral` of the provider string mapping for a default value with a line break on a MySQL column that is not TEXT/BLOB/JSON | `DEFAULT CONCAT('a', CHAR(10), 'b')`, error 1064 | `MySqlStringTypeMapping.Clone(replaceLineBreaksWithCharFunction: false)` before `GenerateSqlLiteral` |
| MySQL query-based bulk update whose source query has a subquery on the target table | Error 1093 "You can't specify target table '...' for update in FROM clause" | Remove the subquery on the target from the source query |
| MySQL temp table string column without the charset and collation of the model | Error 1267 (illegal mix of collations) in joins and comparisons | Read charset and collation from the design-time relational model (`MySqlColumnCharSet`) |
| Provider literal for a `TimeSpan` constant or a `TimeSpan` default value (`GenerateSqlLiteral`) | 36 hours become `'12:00:00'`, a negative value loses its sign, no error | Write the text with `MySqlTimeFormatter`: `ThinktectureMySqlQuerySqlGenerator.VisitSqlConstant` (`GenerateTimeLiteral`) for a constant, `MySqlTempTableCreator` for a default value |
| JSON value with a UTC offset in `JSON_TABLE ... datetime(6)` | MySQL converts it into the session time zone; `Contains` and joins match no row in a non-UTC session | Serialize `DateTimeOffset` as UTC without an offset (`DateTimeOffsetStringConverter`) |
| `CREATE TEMPORARY TABLE` with the name of a model table on MySQL | The temp table hides the base table for the connection; writes land in the temp table and are lost | `MySqlTempTableCreator.EnsureNoModelTableIsHidden` throws `NotSupportedException` |
| `GetService<IDesignTimeModel>().Model` for every MySQL temp table or collection parameter | Full design-time model build with a compiled model, exception under NativeAOT | Request it only when `MySqlColumnCharSet.SupportsCharSet(columnType)` |
| Staging `CREATE` outside the `try` that drops the table | After a cancelled `CREATE` the next call on the same open connection fails with error 1050 | Keep the `CREATE` inside that `try` |
| `ExecuteSqlRawAsync(sql, cancellationToken)` for SQL text that the library builds | `FormatException` "Input string was not in a correct format" for a brace in a default value or an identifier; `{{` is sent as `{` without an error | `BulkOperationsSqlExecutor.ExecuteNonQueryAsync(ctx.Database, sql, cancellationToken)` (`Thinktecture.Internal`) |
| Embedding a column name, a column type or a JSON property name into a `FromSqlRaw` statement without doubling its braces | `FormatException` at query execution for `HasColumnName("a{b}")`; a name that contains `{0}` or `{1}` gets the parameter placeholder in its place | `EscapeBraces` for every embedded fragment in the collection parameter factories; only the placeholders `{0}` and `{1}` keep single braces |

## Testing

### Test Projects

| Test Project | Tests For | Database |
|-------------|-----------|----------|
| `Relational.Tests` | Relational package | SQLite (in-memory) |
| `BulkOperations.Tests` | Bulk ops abstractions | SQLite (in-memory) |
| `SqlServer.Tests` | SQL Server features | SQL Server (Testcontainers or configured) |
| `PostgreSQL.Tests` | PostgreSQL features | PostgreSQL (Testcontainers or configured) |
| `MySql.Tests` | MySQL features | MySQL 8.4 (Testcontainers or configured) |
| `Sqlite.Tests` | SQLite features | SQLite (in-memory) |
| `Testing.Tests` | Testing infrastructure | Varies |
| `TestHelpers` | Shared entities & DbContext (not a test project) | N/A |

### Naming & Structure

- Test methods: `Should_DoSomething` or `Should_DoSomething_When_Condition`
- `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterized
- Always `async Task` return (not `void`)
- A new provider test suite mirrors the directory structure, the test isolation and the feature coverage of the mature provider suites. Behavior that is explicitly provider-specific is not copied.

### AAA Pattern with Three DbContexts

All integration tests use `ArrangeDbContext` (setup), `ActDbContext` (execute), `AssertDbContext` (verify) — same database, independent change trackers, provided by `TestCtxProvider`.
The PostgreSQL and MySQL tests provide the three contexts through their base classes (`IntegrationTestsBase`, `MySqlIntegrationTestsBase`) and capture the executed SQL with `CommandCapturingInterceptor`.

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

#### MySQL

```csharp
[Collection(MySqlTestCollection.NAME)]
public sealed class MyTests : MySqlIntegrationTestsBase
{
   public MyTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }
}
```

- Inherits `MySqlIntegrationTestsBase` (implements `IAsyncLifetime, IAsyncDisposable`); the collection `MySqlTestCollection.NAME` (`"MySqlTests"`) shares one `MySqlContainerFixture` and disables parallelization.
- `ArrangeDbContext`, `ActDbContext`, `AssertDbContext` are lazy `MySqlTestDbContext` instances. `ExecutedCommands` comes from `CommandCapturingInterceptor.Commands`; it also contains the setup statements, so the query-based test classes pick a statement by its start (private helper `GetExecutedStatement("UPDATE ")`) instead of `ExecutedCommands.Last()`.
- Per-test config: `ConfigureModel`, `Configure` and `ConfigureMySql` (`Action<MySqlDbContextOptionsBuilder>`, e.g. `AddCollectionParameterSupport()`), set before the first context access. `UseBulkCopy = true` registers `AddBulkOperationSupport(useBulkCopy: true)` and adds `AllowLoadLocalInfile=true` to the connection string. The base contexts register `AddBulkOperationSupport()`, `AddWindowFunctionsSupport()` and `AddNestedTransactionSupport()`, and the logger writes to `ITestOutputHelper`.
- `MySqlTestDbContext.CreateOptionsBuilder()` ignores `CoreEventId.ManyServiceProvidersCreatedWarning`, because the tests create many option combinations. A test with `ConfigureModel` replaces `IModelCacheKeyFactory` with `CachePerContextModelCacheKeyFactory`.
- Isolation: `InitializeAsync()` runs `EnsureCreatedAsync()` and truncates every non-empty table of the database (`SET FOREIGN_KEY_CHECKS = 0`), which also resets AUTO_INCREMENT counters. `EnsureCreatedAsync` does nothing when the database already has tables, so drop the configured database after a model change (same limit as the PostgreSQL tests).

**Template:** `tests/Thinktecture.EntityFrameworkCore.MySql.Tests/EntityFrameworkCore/MySqlIntegrationTestsBase.cs`

### How to Access the SUT and Services in Tests

| Need | How |
|------|-----|
| Bulk operation executor | `ActDbContext.GetService<IBulkInsertExecutor>()` (or provider-specific type) |
| Temp table creator | `ActDbContext.GetService<ITempTableCreator>()` |
| Per-test model config | Set `ConfigureModel = modelBuilder => { ... };` **before** first DbContext access |
| Per-test options config | Set `Configure = optionsBuilder => { ... };` **before** first DbContext access |
| Capture executed SQL | SQL Server/SQLite: `TestCtxProvider.ExecutedCommands`; PostgreSQL/MySQL: `_commandCapturingInterceptor.Commands` |
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

Shared in `tests/Thinktecture.EntityFrameworkCore.TestHelpers/TestDatabaseContext/`: `TestEntity`, `TestEntityWithAutoIncrement`, `TestEntityWithRowVersion`, `TestEntityWithShadowProperties`, `TestEntityWithSqlDefaultValues`, `TestEntityWithComplexType`, `TestEntityWithNullableComplexTypes`, `TestEntityWithStructsInOptionalComplexType` (struct complex types inside an optional class; `StructColumns`/`QueryStructColumns` read the member columns with raw SQL), `KeylessTestEntity`, `TestEntityWithJsonColumns`, `OwnedEntity` and variants. Each has a static `Configure(ModelBuilder)` method. Each provider's test project has its own `TestDbContext`. The MySQL test project uses `MySqlTestDbContext` with the shared entities (`TestEntityWithRowVersion` is SQL Server only) and its own entities (`MySqlTestEntity`, `MySqlCompositeEntity`, `MySqlQuerySource`, `MySqlQueryTarget`, `MySqlTypedEntity`, `MySqlTemporalEntity`, `MySqlOwnerEntity`, `MySqlCollatedEntity`, `MySqlAutoIncrementEntity` and others); `ConfigureSharedEntities` configures the MySQL equivalent where a shared entity uses a provider-specific feature.

### Connection Configuration

- SQL Server: `appsettings.json` with `UseSqlServerContainer` + `ConnectionStrings:default`
- PostgreSQL: `appsettings.json` with `UsePostgreSqlContainer` + `ConnectionStrings:default`
- MySQL: `appsettings.json` with `UseMySqlContainer` (default `true`: Testcontainers `mysql:8.4` with `--local-infile=1`; the EF server version is `MySqlContainerFixture.ServerVersion`) + `ConnectionStrings:default` (a dedicated database; the default value uses `root` and `AllowLoadLocalInfile=true`). Environment variables override both (`UseMySqlContainer=false`, `ConnectionStrings__default=...`). A configured server needs `local_infile=ON`, because the bulk copy tests use `LOAD DATA LOCAL INFILE`.
- SQL Server uses `OneTimeMigrationStrategy`; PostgreSQL uses `EnsureCreatedAsync` with schema isolation; MySQL uses `EnsureCreatedAsync` in one database and truncates the non-empty tables before each test

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
- SQL Server: `UseSqlServerContainer=true`; PostgreSQL: `UsePostgreSqlContainer=true`; MySQL: `UseMySqlContainer=true` (the default of `appsettings.json`)
- NuGet published to nuget.org on git tags
- Test results: `test-results/<TargetFramework>/` as TRX files
