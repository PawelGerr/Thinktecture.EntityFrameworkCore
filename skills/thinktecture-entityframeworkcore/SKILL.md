---
name: thinktecture-entityframeworkcore
description: >-
  Use when writing or reviewing C# Entity Framework Core code that uses the
  Thinktecture.EntityFrameworkCore packages — bulk insert/update/upsert, bulk
  insert/update from a query, temp tables, collection parameters, window
  functions (RowNumber / NTile / aggregates), table hints, nested transactions,
  truncate, multi-tenant databases, runtime schema changes, or migration helpers
  (include columns, clustered PK, IF [NOT] EXISTS) on SQL Server, PostgreSQL, or
  SQLite.
---

# Thinktecture.EntityFrameworkCore

A set of EF Core extension packages that add high-performance and convenience
features missing from EF Core itself: **bulk operations**, **temp tables**,
**collection parameters**, **window functions**, **table hints**, **nested
(virtual) transactions**, **dedicated truncate**, **per-tenant databases**, and
**runtime schema changes**.

This skill teaches you (the AI agent) how to **consume** these packages in an
application. It is Markdown guidance only — there is no code to run.

> The library targets **EF Core 10** and **.NET 10**. All public extension
> methods live in the root `Thinktecture` namespace, so a single
> `using Thinktecture;` unlocks the entire API surface.

---

## 1. Installation — pick the package for your provider

Install **one** provider package. Each one transitively pulls in the shared
`BulkOperations` and `Relational` layers — do **not** add those by hand.

| Provider | NuGet package | Pulls in |
|----------|---------------|----------|
| SQL Server | `Thinktecture.EntityFrameworkCore.SqlServer` | `…BulkOperations` → `…Relational` + `Microsoft.EntityFrameworkCore.SqlServer` |
| PostgreSQL | `Thinktecture.EntityFrameworkCore.PostgreSQL` | `…BulkOperations` → `…Relational` + `Npgsql.EntityFrameworkCore.PostgreSQL` |
| SQLite | `Thinktecture.EntityFrameworkCore.Sqlite` | `…Sqlite.Core` → `…BulkOperations` → `…Relational` + `Microsoft.EntityFrameworkCore.Sqlite` |

Test helpers live in separate `*.Testing` packages
(`Thinktecture.EntityFrameworkCore.SqlServer.Testing`, `…Sqlite.Testing`).

> The `*.Testing` packages are de-emphasized — some of their APIs are marked
> obsolete (in favour of `ITestIsolationOptions`-based overloads). For new
> projects prefer your own test-context setup (e.g. Testcontainers).

---

## 2. Setup — enable the features you use

Features are **opt-in**. You enable them on the provider's options builder inside
`UseSqlServer` / `UseNpgsql` / `UseSqlite`, or on the outer
`DbContextOptionsBuilder` for the provider-agnostic ones. **A feature that is not
enabled here will throw or silently fail at the call site** — this is the #1
consumer mistake.

```csharp
using Thinktecture; // required for every extension below

services.AddDbContext<MyDbContext>(builder => builder
   .UseSqlServer(connectionString, sql => sql
      .AddBulkOperationSupport()      // bulk insert/update/upsert + temp tables
      .AddWindowFunctionsSupport()    // RowNumber, NTile, windowed aggregates
      .AddCollectionParameterSupport()// pass collections as a query parameter
      .AddTableHintSupport())         // WithTableHints(NoLock, …)  [SQL Server only]
   .AddNestedTransactionSupport());   // provider-agnostic, on the outer builder
```

### Feature-flag → provider availability

| Options-builder method | SQL Server | PostgreSQL | SQLite | Enables |
|------------------------|:---:|:---:|:---:|---------|
| `AddBulkOperationSupport()` | ✔ | ✔ | ✔ | Bulk insert/update/upsert, temp tables, truncate |
| `AddWindowFunctionsSupport()` | ✔ | ✔ | ✔ ² | `EF.Functions.RowNumber/NTile/…` |
| `AddCollectionParameterSupport()` | ✔ | ✔ | — | Scalar/JSON collection parameters |
| `AddTableHintSupport()` | ✔ | — | — | `query.WithTableHints(…)` |
| `AddTenantDatabaseSupport<TFactory>()` | ✔ | — | — | Per-tenant database names |
| `AddNestedTransactionSupport()` ¹ | ✔ | ✔ | ✔ | Virtual nested transactions |
| `AddSchemaRespectingComponents()` ¹ | ✔ | ✔ | ✔ | Change default schema at runtime |
| `UseThinktecture{Provider}MigrationsSqlGenerator()` | ✔ | ✔ | — | Migration helpers (include cols, clustered PK, IF [NOT] EXISTS) |

¹ Provider-agnostic — call on the outer `DbContextOptionsBuilder`, not the
provider sub-builder.

² On **SQLite**, window functions are limited to `RowNumber` and `NTile`; the
windowed aggregates (`Sum`/`Average`/`Min`/`Max`) are **SQL Server & PostgreSQL
only**.

`AddBulkOperationSupport()` and `AddCollectionParameterSupport()` take a
`configureTempTablesForPrimitiveTypes` / `configureCollectionParametersForPrimitiveTypes`
flag (default `true`) that auto-registers temp-table/collection mappings for
primitive types like `int`, `string`, `Guid`.

### Minimal end-to-end example (zero to working)

Two pieces must line up: the **feature flag** (options) and the **call site**.
A plain bulk insert into a regular entity needs nothing else:

```csharp
using Thinktecture;

class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
   public DbSet<Product> Products => Set<Product>();
}

services.AddDbContext<AppDbContext>(builder => builder
   .UseSqlServer(connectionString, sql => sql.AddBulkOperationSupport()));

// Call site — no OnModelCreating change required for a regular entity:
await ctx.BulkInsertAsync(products);   // → references/Bulk-Insert.md
```

A **third** piece — model registration in `OnModelCreating` — is required only
for features that introduce new entity shapes: temp tables
(`modelBuilder.ConfigureTempTableEntity<T>()` → references/Temp-Tables.md) and complex
collection parameters (`ConfigureComplexCollectionParameter<T>()` →
references/Collection-Parameters.md). Each reference file calls out its own setup step.

---

## 3. Feature catalog → reference files

Read the matching file in `references/` before generating non-trivial code.

Provider availability for each feature is in the matrices in §2 and §6.

| Feature | Reference |
|---------|-----------|
| Bulk insert (from in-memory entities) | `references/Bulk-Insert.md` |
| Bulk update (from in-memory entities) | `references/Bulk-Update.md` |
| Bulk insert-or-update (upsert) | `references/Bulk-Insert-or-Update.md` |
| Bulk **insert from a query** | `references/Bulk-Insert-from-Query.md` |
| Bulk **update from a query** | `references/Bulk-Update-from-Query.md` |
| Temp tables | `references/Temp-Tables.md` |
| Collection parameters ("temp tables light") | `references/Collection-Parameters.md` |
| Window functions (RowNumber/NTile/aggregates) | `references/Window-Functions.md` |
| Table hints (`NOLOCK`, …) | `references/Table-Hints.md` |
| Nested (virtual) transactions | `references/Nested-Transactions.md` |
| Truncate table | `references/Truncate-Table.md` |
| Per-tenant / cross-database queries | `references/Multi-Tenant-Databases.md` |
| Change default schema at runtime | `references/Runtime-Schema.md` |
| Migration helpers (include cols, clustered PK, IF [NOT] EXISTS, identity) | `references/Migrations.md` |

---

## 4. Choosing the right approach

Several features overlap. Pick deliberately:

### Writing many rows

```
Source of the rows?
├─ In-memory collection (List<T>, array) you already have
│   ├─ Insert only ............... ctx.BulkInsertAsync(entities)            → references/Bulk-Insert.md
│   ├─ Update only ............... ctx.BulkUpdateAsync(entities)            → references/Bulk-Update.md
│   └─ Insert-or-update ......... ctx.BulkInsertOrUpdateAsync(entities)     → references/Bulk-Insert-or-Update.md
└─ The result of a LINQ query (server-side, no round-trip to the app)
    ├─ Insert .................... dbSet.BulkInsertAsync(sourceQuery, map)  → references/Bulk-Insert-from-Query.md
    └─ Update .................... dbSet.BulkUpdateAsync(sourceQuery, …)    → references/Bulk-Update-from-Query.md
```

- **Bulk from data** (`SqlBulkCopy` / COPY / batched INSERT) is the fastest way
  to push a large in-memory collection into the database.
- **Bulk from query** keeps everything server-side
  (`INSERT … SELECT …` / `UPDATE … FROM …`) — use it when the source rows come
  from the database itself; no data travels to the app and back.
- **EF Core's native `ExecuteUpdateAsync`/`ExecuteDeleteAsync`** is the right
  choice for a *single-table* set-based update with a simple `Where`. Reach for
  **Bulk-Update-from-Query** instead when the update must **join another query**
  (set target columns from a related source set) or needs composite-key joins.

### Passing a list into a query (e.g. `WHERE Id IN (…)`)

```
How big is the list / how often does the query run?
├─ Small, ad-hoc ............... EF Core's native  .Where(e => ids.Contains(e.Id))
├─ Large or reused, want an
│  indexable, join-able set .... Collection Parameters  → references/Collection-Parameters.md
│                                (ctx.CreateScalarCollectionParameter<T>() /
│                                 CreateComplexCollectionParameter<T>())
└─ Need a real (multi-column,
   indexed, primary-keyed) set
   to JOIN against repeatedly ... Temp Tables  → references/Temp-Tables.md
```

- **Collection parameters** ("temp tables light") need **no `CREATE TABLE`** and
  no disposal — they serialize the list into a single query parameter that the
  database unfolds into a rowset. Best for read-side `JOIN` / `IN` against a list.
- **Temp tables** are heavier (DDL + bulk insert + cleanup via
  `await using`) but give you a real, indexable, multi-column table you can join,
  update, and reuse across multiple queries in the same context.

### Deleting all rows of a table

Use the dedicated **`TruncateTableAsync<T>()`** (→ `Truncate-Table.md`), not
`ExecuteDeleteAsync()` — truncate is dramatically faster and resets identity.
Caveats: on **SQL Server**, `TRUNCATE` fails if the table is referenced by a
foreign key; on **PostgreSQL**, pass `cascade: true` to truncate an
FK-referenced table; on **SQLite** it falls back to `DELETE FROM`.

---

## 5. Common pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Feature not enabled in options | `InvalidOperationException` / method missing at runtime | Add the matching `Add…Support()` in `UseXxx(...)` (§2) |
| Temp-table / collection entity not registered | `ArgumentException: The provided type T is not known…`, or column names wrong | Call `modelBuilder.ConfigureTempTableEntity<T>()` / `ConfigureTempTable<…>()` in `OnModelCreating` (→ references/Temp-Tables.md) |
| Querying a temp table after it's disposed | `Invalid object name '#…'` | Keep the `await using` scope open while you query `.Query` |
| Using a feature on the wrong provider | Compile error or `NotSupportedException` | Check the Provider column in §3 and the per-feature reference |
| Expecting `BulkInsertAsync(query, …)` to take a `List<T>` | Wrong overload / compile error | `DbContext.BulkInsertAsync(collection)` vs `DbSet.BulkInsertAsync(IQueryable source, map)` are different APIs (§4) |
| Forgetting `using Thinktecture;` | Extension methods not found | Add the single `using Thinktecture;` |
| Window function used directly in `Where` | EF cannot translate / client evaluation | Project the window result, call `.AsSubQuery()`, then `Where` on it (→ references/Window-Functions.md) |
| Windowed aggregate on SQLite | `Sum`/`Average`/`Min`/`Max` fail to translate | SQLite supports only `RowNumber`/`NTile`; compute the aggregate with a correlated subquery or in application code, or run on SQL Server/PostgreSQL for native windowed aggregates |
| Owned types in bulk **update**/**upsert** | `NotSupportedException` ("Temp tables don't support owned entities") | Bulk insert supports owned entities; update/upsert do not — update those columns via native `ExecuteUpdateAsync`, or flatten the owned type into scalar properties (→ references/Bulk-Update.md, references/Bulk-Insert-or-Update.md) |
| Truncating an FK-referenced table | `TRUNCATE` fails (SQL Server) | Remove/disable the FK, or use PostgreSQL `cascade: true`; SQLite uses `DELETE FROM` and is unaffected |

---

## 6. Provider feature matrix (quick reference)

| Feature | SQL Server | PostgreSQL | SQLite |
|---------|:---:|:---:|:---:|
| Bulk Insert / Update / Upsert | ✔ | ✔ | ✔ |
| Bulk Insert / Update from Query | ✔ | ✔ | ✔ |
| Temp Tables | ✔ | ✔ | ✔ |
| Collection Parameters | ✔ | ✔ | — |
| Window Functions | ✔ | ✔ | ✔ ¹ |
| Table Hints | ✔ | — | — |
| Nested Transactions | ✔ | ✔ | ✔ |
| Tenant Databases | ✔ | — | — |
| Truncate Table | ✔ | ✔ | ✔ |
| Runtime Schema Change | ✔ | ✔ | ✔ |

¹ SQLite window functions are limited to `RowNumber`/`NTile` — see the §2
footnote for details.

For version-accurate documentation snippets you can also use the **Context7
MCP** server with library id `/pawelgerr/thinktecture.entityframeworkcore`.
