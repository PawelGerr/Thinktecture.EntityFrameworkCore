# Bulk Update (from in-memory entities)

Update a large in-memory collection in one fast operation. The rows are pushed
into a temp table and then applied to the target table: **SQL Server** uses a
`MERGE`, **PostgreSQL** uses `UPDATE … FROM <temp table>`, **SQLite** uses
batched `UPDATE` statements. Returns the number of affected rows. (Owned entity
types are **not** supported — see the notes below.)

> This page is for updating a collection you already hold in memory
> (`List<T>`, array, …) — every entity carries the new values. To update rows
> from the result of a server-side LINQ query (no round-trip to the app, set
> target columns from a related source set), see `Bulk-Update-from-Query.md`.

**Enable:** `AddBulkOperationSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;`

## API

The method is on `DbContext` and is the **same for every provider**:

```csharp
Task<int> BulkUpdateAsync<T>(
   this DbContext ctx,
   IEnumerable<T> entities,
   Expression<Func<T, object?>>? propertiesToUpdate = null,
   Expression<Func<T, object?>>? propertiesToMatchOn = null,
   CancellationToken cancellationToken = default) where T : class;

Task<int> BulkUpdateAsync<T>(
   this DbContext ctx,
   IEnumerable<T> entities,
   IBulkUpdateOptions options,
   CancellationToken cancellationToken = default) where T : class;
```

- `propertiesToUpdate` — which columns to write. `null` ⇒ all properties.
- `propertiesToMatchOn` — the **match key** (the `JOIN`/`ON` columns). `null` ⇒
  the entity's primary key. In the options object this is the `KeyProperties`
  property.

Only the **options type** differs per provider (all implement `IBulkUpdateOptions`,
which exposes `PropertiesToUpdate`, `KeyProperties`, `TableName`, `Schema`):

| Provider | Options type | Notable extra properties |
|----------|--------------|--------------------------|
| SQL Server | `SqlServerBulkUpdateOptions` | `MergeTableHints`, `TempTableOptions` (`BatchSize`, `EnableStreaming`, `BulkCopyTimeout`, `SqlBulkCopyOptions`) |
| PostgreSQL | `NpgsqlBulkUpdateOptions` | `TempTableOptions` |
| SQLite | `SqliteBulkUpdateOptions` | `AutoIncrementBehavior` |

## Examples

### Simplest — update everything, match on the primary key

```csharp
using Thinktecture;

var products = new List<Product>
{
   new() { Id = 1, Name = "Widget", Price = 19.99m },
   new() { Id = 2, Name = "Gadget", Price = 39.99m },
};

int affected = await ctx.BulkUpdateAsync(products);
```

### Update only selected columns

The first projection selects the columns to **write**:

```csharp
// only Price is sent to the DB; match still uses the primary key
await ctx.BulkUpdateAsync(products, p => p.Price);

// equivalent with an anonymous type for multiple columns
await ctx.BulkUpdateAsync(products, p => new { p.Name, p.Price });
```

### Custom match key

```csharp
// update Price, but match rows on Sku instead of the primary key
await ctx.BulkUpdateAsync(
   products,
   propertiesToUpdate: p => p.Price,
   propertiesToMatchOn: p => p.Sku);
```

### With provider options

```csharp
// SQL Server — select columns + match key, tune the temp-table bulk copy
await ctx.BulkUpdateAsync(products, new SqlServerBulkUpdateOptions
{
   PropertiesToUpdate = IEntityPropertiesProvider.Include<Product>(p => p.Price),
   KeyProperties      = IEntityPropertiesProvider.Include<Product>(p => p.Id),
   MergeTableHints    = { SqlServerTableHintLimited.HoldLock, SqlServerTableHintLimited.RowLock },
   TempTableOptions   =
   {
      BatchSize = 5_000,
      EnableStreaming = true,
      BulkCopyTimeout = TimeSpan.FromSeconds(30),
   },
});

// PostgreSQL
await ctx.BulkUpdateAsync(products, new NpgsqlBulkUpdateOptions
{
   PropertiesToUpdate = IEntityPropertiesProvider.Include<Product>(p => p.Price),
   KeyProperties      = IEntityPropertiesProvider.Include<Product>(p => p.Id),
});

// SQLite
await ctx.BulkUpdateAsync(products, new SqliteBulkUpdateOptions
{
   PropertiesToUpdate    = IEntityPropertiesProvider.Include<Product>(p => p.Price),
   KeyProperties         = IEntityPropertiesProvider.Include<Product>(p => p.Id),
   AutoIncrementBehavior = SqliteAutoIncrementBehavior.KeepValueAsIs,
});
```

### Exclude a few columns instead of listing all of them

```csharp
await ctx.BulkUpdateAsync(products, new SqlServerBulkUpdateOptions
{
   PropertiesToUpdate = IEntityPropertiesProvider.Exclude<Product>(p => p.CreatedAt),
});
```

### Override the target table

```csharp
await ctx.BulkUpdateAsync(products, new SqlServerBulkUpdateOptions
{
   Schema = "archive",
   TableName = "ArchivedProducts",
});
```

## Notes & pitfalls

- **Enable the feature** (`AddBulkOperationSupport()`), or the method throws at
  runtime.
- The **match key** is `propertiesToMatchOn` on the convenience overload, and
  `KeyProperties` on the options object — same thing. `null`/unset ⇒ the entity's
  primary key.
- `propertiesToUpdate` / `PropertiesToUpdate` lists the columns to **write**; use
  `IEntityPropertiesProvider.Exclude<T>(…)` to write everything except a few.
- The mechanism differs by provider: SQL Server runs a `MERGE` (entities go into a
  temp table first), PostgreSQL runs `UPDATE … FROM <temp table>`, SQLite issues
  batched `UPDATE` statements. The return value is the number of affected rows.
- Table/schema override only changes the **target** table; column names are still
  resolved from the EF Core model, so the override target must have compatible
  columns.
- **Owned entity types are not supported.** Entities with shadow properties must
  be attached to the `DbContext` so the values can be read.
  **Workaround:** update the owned/mapped columns separately with EF Core's native
  `ExecuteUpdateAsync` (a single-table set-based update), or restructure the model
  so those columns are regular scalar properties rather than an owned type.
- To update from a server-side query instead of in-memory data (e.g. set target
  columns from a joined source set), use **`DbSet<T>.BulkUpdateAsync(IQueryable source, …)`**
  — a different API — see `Bulk-Update-from-Query.md`.
