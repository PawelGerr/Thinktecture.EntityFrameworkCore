# Bulk Insert-or-Update / Upsert (from in-memory entities)

Insert rows that don't exist yet and update the ones that do, in one operation.
The in-memory collection is pushed into a temp table and merged into the target:
**SQL Server** uses a `MERGE`, **PostgreSQL** uses `INSERT … ON CONFLICT … DO
UPDATE`, **SQLite** uses batched `INSERT`/`UPDATE` statements. Returns the number
of affected rows (inserted + updated). (Owned entity types are **not**
supported — see the notes below.)

> This page is for upserting a collection you already hold in memory
> (`List<T>`, array, …). For a pure insert use `Bulk-Insert.md`; for a pure
> update use `Bulk-Update.md`. To update rows from a server-side LINQ query, see
> `Bulk-Update-from-Query.md`.

**Enable:** `AddBulkOperationSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;`

## API

The method is on `DbContext` and is the **same for every provider**:

```csharp
Task<int> BulkInsertOrUpdateAsync<T>(
   this DbContext ctx,
   IEnumerable<T> entities,
   Expression<Func<T, object?>>? propertiesToInsert = null,
   Expression<Func<T, object?>>? propertiesToUpdate = null,
   Expression<Func<T, object?>>? propertiesToMatchOn = null,
   CancellationToken cancellationToken = default) where T : class;

Task<int> BulkInsertOrUpdateAsync<T>(
   this DbContext ctx,
   IEnumerable<T> entities,
   IBulkInsertOrUpdateOptions options,
   CancellationToken cancellationToken = default) where T : class;
```

- `propertiesToInsert` — columns written for **new** rows. `null` ⇒ all properties.
- `propertiesToUpdate` — columns written for **existing** rows. `null` ⇒ all
  properties. Pass `IEntityPropertiesProvider.Empty` (options only) to skip the
  UPDATE entirely (insert-only / "do nothing on conflict").
- `propertiesToMatchOn` — the **match key** that decides insert-vs-update. `null`
  ⇒ the entity's primary key. In the options object this is `KeyProperties`.

Only the **options type** differs per provider (all implement
`IBulkInsertOrUpdateOptions`, which exposes `PropertiesToInsert`,
`PropertiesToUpdate`, `KeyProperties`, `TableName`, `Schema`):

| Provider | Options type | Notable extra properties |
|----------|--------------|--------------------------|
| SQL Server | `SqlServerBulkInsertOrUpdateOptions` | `MergeTableHints`, `TempTableOptions` (`BatchSize`, `EnableStreaming`, `BulkCopyTimeout`, `SqlBulkCopyOptions`) |
| PostgreSQL | `NpgsqlBulkInsertOrUpdateOptions` | `ConflictDoNothing`, `TempTableOptions` |
| SQLite | `SqliteBulkInsertOrUpdateOptions` | `AutoIncrementBehavior` |

## Examples

### Simplest — upsert everything, match on the primary key

```csharp
using Thinktecture;

var products = new List<Product>
{
   new() { Id = 1, Name = "Widget", Price = 19.99m },   // exists -> updated
   new() { Id = 2, Name = "Gadget", Price = 39.99m },   // new    -> inserted
};

int affected = await ctx.BulkInsertOrUpdateAsync(products);
```

### Select insert / update / match columns

```csharp
await ctx.BulkInsertOrUpdateAsync(
   products,
   propertiesToInsert: p => new { p.Id, p.Name, p.Price },
   propertiesToUpdate: p => p.Price,   // only Price refreshed on existing rows
   propertiesToMatchOn: p => p.Id);
```

### With provider options

```csharp
// SQL Server — subset of columns + match key, MERGE hints, temp-table tuning
await ctx.BulkInsertOrUpdateAsync(products, new SqlServerBulkInsertOrUpdateOptions
{
   PropertiesToInsert = IEntityPropertiesProvider.Include<Product>(p => new { p.Id, p.Name, p.Price }),
   PropertiesToUpdate = IEntityPropertiesProvider.Include<Product>(p => p.Price),
   KeyProperties      = IEntityPropertiesProvider.Include<Product>(p => p.Id),
   MergeTableHints    = { SqlServerTableHintLimited.HoldLock },
   TempTableOptions   = { BatchSize = 5_000, EnableStreaming = true },
});

// PostgreSQL — INSERT ... ON CONFLICT DO UPDATE
await ctx.BulkInsertOrUpdateAsync(products, new NpgsqlBulkInsertOrUpdateOptions
{
   PropertiesToUpdate = IEntityPropertiesProvider.Include<Product>(p => p.Price),
   KeyProperties      = IEntityPropertiesProvider.Include<Product>(p => p.Id),
});

// SQLite
await ctx.BulkInsertOrUpdateAsync(products, new SqliteBulkInsertOrUpdateOptions
{
   PropertiesToUpdate    = IEntityPropertiesProvider.Include<Product>(p => p.Price),
   KeyProperties         = IEntityPropertiesProvider.Include<Product>(p => p.Id),
   AutoIncrementBehavior = SqliteAutoIncrementBehavior.KeepValueAsIs,
});
```

### Insert-only (skip the update on conflict)

Provide **zero** properties to update — the UPDATE part is skipped entirely, so
existing rows are left untouched and only new rows are inserted:

```csharp
await ctx.BulkInsertOrUpdateAsync(products, new SqlServerBulkInsertOrUpdateOptions
{
   PropertiesToUpdate = IEntityPropertiesProvider.Empty,
});
```

On PostgreSQL the dedicated `ConflictDoNothing` flag does the same via
`ON CONFLICT … DO NOTHING`:

```csharp
await ctx.BulkInsertOrUpdateAsync(products, new NpgsqlBulkInsertOrUpdateOptions
{
   ConflictDoNothing = true,   // silently skip conflicting rows instead of updating
});
```

### Override the target table

```csharp
await ctx.BulkInsertOrUpdateAsync(products, new SqlServerBulkInsertOrUpdateOptions
{
   Schema = "archive",
   TableName = "ArchivedProducts",
});
```

## Notes & pitfalls

- **Enable the feature** (`AddBulkOperationSupport()`), or the method throws at
  runtime.
- The **match key** decides insert vs. update: `propertiesToMatchOn` on the
  convenience overload, `KeyProperties` on the options object. `null`/unset ⇒ the
  entity's primary key.
- `PropertiesToInsert` and `PropertiesToUpdate` are independent — list the insert
  columns and the (typically smaller) set of columns to refresh on existing rows
  separately. Use `IEntityPropertiesProvider.Exclude<T>(…)` to keep all but a few.
- **Insert-only:** set `PropertiesToUpdate = IEntityPropertiesProvider.Empty`
  (all providers) or `ConflictDoNothing = true` (PostgreSQL only).
- The mechanism differs by provider: SQL Server `MERGE` (entities staged in a temp
  table), PostgreSQL `INSERT … ON CONFLICT … DO UPDATE`, SQLite batched
  `INSERT`/`UPDATE`. The return value is the total affected rows (inserted +
  updated).
- Table/schema override only changes the **target** table; column names are still
  resolved from the EF Core model.
- **Owned entity types are not supported.** Entities with shadow properties must
  be attached to the `DbContext` so the values can be read.
  **Workaround:** bulk **insert** supports owned types, so for an upsert split the
  work — `BulkInsertAsync` the new rows and update existing rows' owned columns via
  EF Core's native `ExecuteUpdateAsync` — or restructure the owned type into scalar
  properties.
