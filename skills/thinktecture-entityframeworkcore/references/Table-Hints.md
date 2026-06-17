# Table Hints (SQL Server only)

Attach SQL Server table hints (`NOLOCK`, `UPDLOCK`, `ROWLOCK`, `INDEX(...)`, …)
to the table used by a LINQ query — emitted as `WITH (...)` after the table
reference.

> **SQL Server only.** There is no PostgreSQL or SQLite equivalent. The method
> exists on `IQueryable<T>` for every provider, but only the SQL Server provider
> translates it; the hint values come from the SQL-Server-specific
> `SqlServerTableHint` type.

**Enable:** `AddTableHintSupport()` in `UseSqlServer`.
**Namespace:** `using Thinktecture;` (extension method) and
`using Thinktecture.EntityFrameworkCore;` (the `SqlServerTableHint` type).

## API

The extension method is on `IQueryable<T>`:

```csharp
public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, params ITableHint[] hints);

public static IQueryable<T> WithTableHints<T>(this IQueryable<T> source, IReadOnlyList<ITableHint> hints);
```

The hint values are static members of `SqlServerTableHint` (which implements
`ITableHint`):

| Member | Emits | | Member | Emits |
|--------|-------|---|--------|-------|
| `NoExpand` | `NOEXPAND` | | `ReadPast` | `READPAST` |
| `ForceScan` | `FORCESCAN` | | `ReadUncommitted` | `READUNCOMMITTED` |
| `ForceSeek` | `FORCESEEK` | | `RepeatableRead` | `REPEATABLEREAD` |
| `HoldLock` | `HOLDLOCK` | | `RowLock` | `ROWLOCK` |
| `NoLock` | `NOLOCK` | | `Serializable` | `SERIALIZABLE` |
| `NoWait` | `NOWAIT` | | `Snapshot` | `SNAPSHOT` |
| `PagLock` | `PAGLOCK` | | `TabLock` | `TABLOCK` |
| `ReadCommitted` | `READCOMMITTED` | | `TabLockx` | `TABLOCKX` |
| `ReadCommittedLock` | `READCOMMITTEDLOCK` | | `UpdLock` | `UPDLOCK` |
| | | | `XLock` | `XLOCK` |

Two are methods (take an argument):

```csharp
SqlServerTableHint.Index(string name);                 // INDEX([name]) — name is escaped
SqlServerTableHint.Spatial_Window_Max_Cells(int value);// SPATIAL_WINDOW_MAX_CELLS = value
```

> `SqlServerTableHintLimited` is a **separate** type used by bulk operations
> (`KeepIdentity`, `IgnoreTriggers`, …) — it is *not* accepted by
> `WithTableHints`.

## Examples

### Single hint — `NOLOCK`

```csharp
using Thinktecture;
using Thinktecture.EntityFrameworkCore;

var product = await ctx.Products
                       .WithTableHints(SqlServerTableHint.NoLock)
                       .FirstOrDefaultAsync(p => p.Id == id);
```

```sql
SELECT [p].[Id], [p].[Name], [p].[Price]
FROM [dbo].[Products] AS [p] WITH (NOLOCK)
WHERE [p].[Id] = @__id_0
```

### Multiple hints — pessimistic lock inside a transaction

```csharp
await using var tx = await ctx.Database.BeginTransactionAsync();

var product = await ctx.Products
                       .WithTableHints(SqlServerTableHint.RowLock, SqlServerTableHint.UpdLock)
                       .SingleAsync(p => p.Id == id);
// ... mutate, SaveChanges, commit
```

```sql
FROM [dbo].[Products] AS [p] WITH (ROWLOCK, UPDLOCK)
```

### Force a specific index (name is escaped automatically)

```csharp
var products = await ctx.Products
                        .WithTableHints(SqlServerTableHint.Index("IX_Products_Name"))
                        .Where(p => p.Name == name)
                        .ToListAsync();
```

```sql
FROM [dbo].[Products] AS [p] WITH (INDEX([IX_Products_Name]))
```

### Per-table on a join

Each `WithTableHints(...)` applies only to the table it is called on, so hints
on a join are independent:

```csharp
var query = ctx.Products.WithTableHints(SqlServerTableHint.NoLock)
               .Join(ctx.Categories.WithTableHints(SqlServerTableHint.UpdLock),
                     p => p.CategoryId, c => c.Id, (p, c) => new { p, c });
```

```sql
FROM [dbo].[Products] AS [p] WITH (NOLOCK)
INNER JOIN [dbo].[Categories] AS [c] WITH (UPDLOCK) ON [p].[CategoryId] = [c].[Id]
```

Hints also work on the `.Query` of a temp table and propagate to owned-entity
sub-tables of the hinted entity.

## Notes & pitfalls

- **SQL Server only.** On PostgreSQL/SQLite the call is not translated — don't
  reach for this to get cross-provider locking behavior.
- **Enable the feature** (`AddTableHintSupport()` in `UseSqlServer`), or the
  hint is not applied.
- `NOLOCK` reads are dirty reads (uncommitted data, possible duplicates/skips).
  Use deliberately — it is not a free performance switch.
- Use `SqlServerTableHint` (for queries), **not** `SqlServerTableHintLimited`
  (that one is for bulk-operation options).
- The hint attaches to the table reference the method is called on; chain a
  separate `WithTableHints(...)` per table when you need hints on joined tables.
