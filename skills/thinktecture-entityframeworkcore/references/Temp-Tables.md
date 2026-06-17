# Temp Tables

Create a real, indexable, multi-column temporary table, fill it via bulk insert,
and `JOIN`/query against it from LINQ. Two workflows:

- **All-in-one** — `BulkInsertIntoTempTableAsync` creates the table *and* fills
  it, returning an `ITempTableQuery<T>` whose `.Query` you use immediately.
- **Two-step** — `CreateTempTableAsync<T>` makes an empty table
  (`ITempTableReference`); insert into it later, possibly in several batches.

The temp table is dropped automatically when the returned reference is disposed
(`await using`). Names are auto-suffixed to avoid collisions.

> Lighter alternative: if you only need to pass a list into a query (`IN` / a
> single `JOIN`) and don't need DDL/indexes, prefer **Collection Parameters**
> (`Collection-Parameters.md`).

**Enable:** `AddBulkOperationSupport()`.
**Namespace:** `using Thinktecture;`

## Step 0 (required): register the temp-table entity in `OnModelCreating`

Without this the API throws `ArgumentException: The provided type T is not known…`
or resolves wrong column names. Do this **once** in your `DbContext`.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
   base.OnModelCreating(modelBuilder);

   // Custom entity used only as a temp table.
   // NOTE: this registers it as KEYLESS by default (no PK/index).
   // Pass isKeyless: false if you JOIN on a column and want it indexed/keyed.
   modelBuilder.ConfigureTempTableEntity<MyTempRow>();

   // Scalar/value temp tables (one or two columns):
   modelBuilder.ConfigureTempTable<int>();          // TempTable<int>
   modelBuilder.ConfigureTempTable<int, string>();  // TempTable<int, string>
}
```

`configureTempTablesForPrimitiveTypes` (default `true` on
`AddBulkOperationSupport`) already registers common primitives, so you often
only need `ConfigureTempTableEntity<T>()` for your own row types.

## API

```csharp
// All-in-one (create + insert): returns a queryable temp table.
Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
   this DbContext ctx, IEnumerable<T> entities,
   Expression<Func<T, object?>>? propertiesToInsert = null,
   CancellationToken ct = default) where T : class;

Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
   this DbContext ctx, IEnumerable<T> entities,
   ITempTableBulkInsertOptions? options, CancellationToken ct = default) where T : class;

// Scalar values (1 or 2 columns) — uses the built-in TempTable<...> records.
Task<ITempTableQuery<TColumn1>> BulkInsertValuesIntoTempTableAsync<TColumn1>(
   this DbContext ctx, IEnumerable<TColumn1> values,
   ITempTableBulkInsertOptions? options = null, CancellationToken ct = default);

Task<ITempTableQuery<TempTable<TColumn1, TColumn2>>> BulkInsertValuesIntoTempTableAsync<TColumn1, TColumn2>(
   this DbContext ctx, IEnumerable<(TColumn1, TColumn2)> values,
   ITempTableBulkInsertOptions? options = null, CancellationToken ct = default);

// Two-step: create an empty table, insert later.
Task<ITempTableReference> CreateTempTableAsync<T>(
   this DbContext ctx, ITempTableCreationOptions options, CancellationToken ct = default) where T : class;

Task BulkInsertIntoTempTableAsync<T>(
   this DbContext ctx, IEnumerable<T> entities, ITempTableReference tempTable,
   ITempTableBulkInsertOptions? options = null, CancellationToken ct = default) where T : class;
```

Key types:

| Type | Purpose |
|------|---------|
| `ITempTableQuery<T>` | `IQueryable<T> Query`, `int NumberOfInsertedRows`, plus `Name`; disposable |
| `ITempTableReference` | `string Name`; `IAsyncDisposable` — drops the table on dispose |
| `TempTable<T1>`, `TempTable<T1,T2>` | built-in records for scalar temp tables |

Options per provider (all configure name, PK creation, drop-on-dispose,
truncate-if-exists):

| Provider | Creation options | Bulk-insert options |
|----------|------------------|---------------------|
| SQL Server | `SqlServerTempTableCreationOptions` (`UseDefaultDatabaseCollation`) | `SqlServerTempTableBulkInsertOptions` |
| PostgreSQL | `NpgsqlTempTableCreationOptions` (`SplitCollationComponents`) | `NpgsqlTempTableBulkInsertOptions` (`MomentOfPrimaryKeyCreation`, `Freeze`) |
| SQLite | `TempTableCreationOptions` | `SqliteTempTableBulkInsertOptions` (`AutoIncrementBehavior`) |

## Examples

### All-in-one + join against your own data

```csharp
using Thinktecture;

var rows = new List<MyTempRow> { new(1, "a"), new(2, "b") };

await using ITempTableQuery<MyTempRow> temp =
   await ctx.BulkInsertIntoTempTableAsync(rows);

var joined = await ctx.Products
   .Join(temp.Query, p => p.Id, t => t.Id, (p, t) => new { p.Name, t.Label })
   .ToListAsync();

// temp table is dropped here, at the end of the await using scope
```

### Scalar values (the common `WHERE Id IN (…)` need)

```csharp
var ids = new[] { 1, 2, 3, 4 };

await using var temp = await ctx.BulkInsertValuesIntoTempTableAsync(ids);

var products = await ctx.Products
   .Where(p => temp.Query.Contains(p.Id))
   .ToListAsync();
```

### Two-step (create empty, insert in batches)

```csharp
var options = new SqlServerTempTableCreationOptions { DropTableOnDispose = true };

await using ITempTableReference temp =
   await ctx.CreateTempTableAsync<MyTempRow>(options);

await ctx.BulkInsertIntoTempTableAsync(batch1, temp);
await ctx.BulkInsertIntoTempTableAsync(batch2, temp);

var all = await ctx.Set<MyTempRow>()
   .FromSql($"SELECT * FROM [{temp.Name}]")
   .ToListAsync();
```

> The `[...]` identifier quoting is SQL-Server-specific (PostgreSQL/SQLite use
> `"..."`), and the row type passed to `Set<T>()`/`FromSql` must be a mapped
> queryable entity (e.g. registered via `ConfigureTempTableEntity<T>()`).

### PostgreSQL

```csharp
await using var temp = await ctx.BulkInsertIntoTempTableAsync(rows,
   new NpgsqlTempTableBulkInsertOptions
   {
      MomentOfPrimaryKeyCreation = MomentOfNpgsqlPrimaryKeyCreation.AfterBulkInsert,
   });
```

## Notes & pitfalls

- **Always `await using`** the returned reference/query — otherwise the temp
  table is not dropped, and (with `DropTableOnDispose = false`) name suffixes
  accumulate.
- **Don't query `.Query` after the scope exits** — the table is gone
  (`Invalid object name '#…'`).
- Reusing a table reference across inserts is fine; set
  `TruncateTableIfExists = true` if you intend to refill an existing one.
- `ConfigureTempTableEntity<T>()` registers the row type as **keyless by
  default** (`isKeyless: true`) — the temp table is created without a primary key
  or index. If you `JOIN`/filter on a column and want it indexed, pass
  `isKeyless: false` (and configure the key via the optional `buildAction`).
  Whether/when the PK is physically created is further controlled by the
  bulk-insert options' `PrimaryKeyCreation`.
