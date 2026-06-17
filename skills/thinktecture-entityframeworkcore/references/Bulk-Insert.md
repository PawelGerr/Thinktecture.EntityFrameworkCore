# Bulk Insert (from in-memory entities)

Insert a large in-memory collection in one fast operation:
**SQL Server** uses `SqlBulkCopy`, **PostgreSQL** uses the binary `COPY`
protocol, **SQLite** uses batched `INSERT`. Returns the number of affected rows
(including owned entities).

> This page is for inserting a collection you already hold in memory
> (`List<T>`, array, …). To insert the result of a LINQ query without a
> round-trip to the app, see `Bulk-Insert-from-Query.md`.

**Enable:** `AddBulkOperationSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;`

## API

The method is on `DbContext` and is the **same for every provider**:

```csharp
Task<int> BulkInsertAsync<T>(
   this DbContext ctx,
   IEnumerable<T> entities,
   Expression<Func<T, object?>>? propertiesToInsert = null,
   CancellationToken cancellationToken = default) where T : class;

Task<int> BulkInsertAsync<T>(
   this DbContext ctx,
   IEnumerable<T> entities,
   IBulkInsertOptions options,
   CancellationToken cancellationToken = default) where T : class;
```

Only the **options type** differs per provider (all implement `IBulkInsertOptions`,
which exposes `PropertiesToInsert`, `TableName`, `Schema`):

| Provider | Options type | Notable extra properties |
|----------|--------------|--------------------------|
| SQL Server | `SqlServerBulkInsertOptions` | `BatchSize`, `BulkCopyTimeout`, `SqlBulkCopyOptions`, `EnableStreaming` |
| PostgreSQL | `NpgsqlBulkInsertOptions` | `CommandTimeout`, `Freeze` |
| SQLite | `SqliteBulkInsertOptions` | `AutoIncrementBehavior` |

## Examples

### Simplest — insert everything

```csharp
using Thinktecture;

var products = new List<Product>
{
   new() { Id = 1, Name = "Widget", Price = 29.99m },
   new() { Id = 2, Name = "Gadget", Price = 49.99m },
};

int affected = await ctx.BulkInsertAsync(products);
```

### Insert only selected properties

Pass a projection of the properties to send. (All listed properties are
*included* — the projection selects which columns get written.)

```csharp
await ctx.BulkInsertAsync(products, p => new { p.Id, p.Name });
```

### With provider options

```csharp
// SQL Server — tune SqlBulkCopy
await ctx.BulkInsertAsync(products, new SqlServerBulkInsertOptions
{
   BatchSize = 5000,
   BulkCopyTimeout = TimeSpan.FromSeconds(30),
   PropertiesToInsert = IEntityPropertiesProvider.Include<Product>(p => new { p.Id, p.Name }),
});

// PostgreSQL — FREEZE is a perf win for freshly created/truncated tables
await ctx.BulkInsertAsync(products, new NpgsqlBulkInsertOptions
{
   Freeze = true,
   CommandTimeout = TimeSpan.FromSeconds(60),
});

// SQLite — control auto-increment handling
await ctx.BulkInsertAsync(products, new SqliteBulkInsertOptions
{
   AutoIncrementBehavior = SqliteAutoIncrementBehavior.KeepValueAsIs,
});
```

### Override the target table

```csharp
await ctx.BulkInsertAsync(products, new SqlServerBulkInsertOptions
{
   Schema = "archive",
   TableName = "ArchivedProducts",
});
```

## Notes & pitfalls

- **Enable the feature** (`AddBulkOperationSupport()`), or the method throws at
  runtime.
- The default `AutoIncrementBehavior` for SQLite is `SetZeroToNull` (a `0` key is
  treated as "let the DB assign it"). Use `KeepValueAsIs` to insert explicit `0`s.
- `Freeze = true` (PostgreSQL) is only safe for tables created/truncated in the
  same transaction — it bypasses MVCC visibility.
- Owned entities are inserted too and counted in the returned row count.
- The collection overload is **`DbContext.BulkInsertAsync(collection)`**. The
  similarly-named **`DbSet<T>.BulkInsertAsync(IQueryable source, map)`** is a
  *different* feature (insert from query) — see `Bulk-Insert-from-Query.md`.
