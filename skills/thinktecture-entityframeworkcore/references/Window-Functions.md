# Window Functions

Use SQL window functions (`OVER (...)`) directly inside a LINQ projection via
`EF.Functions`: `RowNumber`, `NTile`, and windowed aggregates (`Sum`, `Average`,
`Min`, `Max`). The `OVER` clause is built fluently with
`EF.Functions.PartitionBy(...)` and `EF.Functions.OrderBy(...)`.

**Enable:** `AddWindowFunctionsSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;` (plus the usual `Microsoft.EntityFrameworkCore` for `EF`).

## Building the `OVER` clause

| Helper | Produces | Chain with |
|--------|----------|-----------|
| `EF.Functions.PartitionBy(col1[, col2 …])` | `WindowFunctionPartitionByClause` | — |
| `EF.Functions.OrderBy(col)` / `OrderByDescending(col)` | `WindowFunctionOrderByClause` | `.ThenBy(col)` / `.ThenByDescending(col)` |

Pass these clauses into the window function. `RowNumber` takes optional partition
columns followed by an order-by clause; aggregates take the column to aggregate
plus partition columns (and an optional order-by clause for running aggregates).

## `RowNumber` — all providers

```csharp
using Thinktecture;

// ROW_NUMBER() OVER (ORDER BY Name)
var ranked = await ctx.Products
   .Select(p => new
   {
      p.Name,
      RowNumber = EF.Functions.RowNumber(EF.Functions.OrderBy(p.Name)),
   })
   .ToListAsync();

// ROW_NUMBER() OVER (PARTITION BY Category ORDER BY Price DESC)
var perCategory = await ctx.Products
   .Select(p => new
   {
      p.Name,
      Rank = EF.Functions.RowNumber(p.Category, EF.Functions.OrderByDescending(p.Price)),
   })
   .ToListAsync();
```

`RowNumber` returns `long`. To filter on it you must first materialize the
projection as a subquery with **`.AsSubQuery()`**, then `Where` on the projected
value — you cannot put a window function directly in a `Where`, and without
`AsSubQuery()` EF Core cannot translate the filter.

```csharp
var top = await ctx.Products
   .Select(p => new { p, Rn = EF.Functions.RowNumber(p.Category, EF.Functions.OrderByDescending(p.Price)) })
   .AsSubQuery()                     // required before filtering on a window result
   .Where(x => x.Rn == 1)            // best-priced product per category
   .Select(x => x.p)
   .ToListAsync();
```

`RowNumber` supports up to **16** partition columns.

## Windowed aggregates — SQL Server & PostgreSQL

```csharp
// AVG(Price) OVER (PARTITION BY Category)
var withAvg = await ctx.Products
   .Select(p => new
   {
      p.Name,
      CategoryAvg = EF.Functions.Average(p.Price, p.Category),
   })
   .ToListAsync();

// Running total: SUM(Price) OVER (PARTITION BY Category ORDER BY CreatedAt)
var running = await ctx.Products
   .Select(p => new
   {
      p.Name,
      RunningTotal = EF.Functions.Sum(p.Price, p.Category, EF.Functions.OrderBy(p.CreatedAt)),
   })
   .ToListAsync();
```

`Sum`/`Average`/`Min`/`Max` support up to 5 partition columns, with or without
an order-by clause. They are **not available on SQLite** — SQLite supports only
`RowNumber` and `NTile`.

## `NTile` — provider-specific

`NTile` lives in each provider's own `DbFunctions` extensions and differs:

| Provider | Return type | `ORDER BY` | Signatures |
|----------|:-----------:|:----------:|-----------|
| SQL Server | `long` | **required** | `NTile(buckets, orderBy)`, `NTile(buckets, partition…, orderBy)` |
| PostgreSQL | `int` | optional | also `NTile(buckets)`, `NTile(buckets, partition…)` |
| SQLite | `int` | optional | also `NTile(buckets)`, `NTile(buckets, partition…)` |

```csharp
// SQL Server — ORDER BY is mandatory
var buckets = await ctx.Products
   .Select(p => new { p.Name, Quartile = EF.Functions.NTile(4, EF.Functions.OrderBy(p.Price)) })
   .ToListAsync();

// PostgreSQL / SQLite — ORDER BY optional, returns int
var pgBuckets = await ctx.Products
   .Select(p => new { p.Name, Bucket = EF.Functions.NTile(4, p.Category) })
   .ToListAsync();
```

## Notes & pitfalls

- **Enable `AddWindowFunctionsSupport()`** or the calls won't translate.
- Window functions can only appear in a **projection** (`Select`). To filter or
  order on the result, project it, call **`.AsSubQuery()`**, then
  `Where`/`OrderBy` on the projected value.
- **Partition-column limits:** `RowNumber` supports up to **16** partition
  columns; `NTile` and the aggregates support up to **5**.
- **SQLite supports only `RowNumber` and `NTile`** — the windowed aggregates
  (`Sum`/`Average`/`Min`/`Max`) are SQL Server & PostgreSQL only. On SQLite,
  compute the aggregate with a correlated subquery or in application code, or run
  the query on SQL Server/PostgreSQL for native windowed aggregates.
- `RowNumber` is `long`; `NTile` is `long` on SQL Server but `int` on
  PostgreSQL/SQLite — match your DTO property type accordingly.
- On SQL Server, `NTile` and ordered aggregates **require** an `OrderBy` clause.
- `AddRowNumberSupport()` is obsolete — use `AddWindowFunctionsSupport()`.
