# Bulk Insert (from a query)

Insert the result of a server-side LINQ query straight into a table, without a
round-trip to the app: generates `INSERT INTO target (cols) SELECT … FROM (subquery) AS s`.
Returns the number of affected rows.

> This page is for inserting the rows produced by an `IQueryable<TSource>`
> (another query, or a temp table). To insert a collection you already hold in
> memory (`List<T>`, array, …) via `SqlBulkCopy` / `COPY` / batched `INSERT`,
> see `Bulk-Insert.md`.

**Enable:** `AddBulkOperationSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;`

## API

The method is on **`DbSet<TTarget>`** (not `DbContext`). The signature is
identical for every provider — only the `options` type differs:

```csharp
// SQL Server
Task<int> BulkInsertAsync<TTarget, TSource>(
   this DbSet<TTarget> target,
   IQueryable<TSource> sourceQuery,
   Func<InsertPropertyBuilder<TTarget, TSource>, InsertPropertyBuilder<TTarget, TSource>> mapPropertyCalls,
   SqlServerBulkInsertFromQueryOptions? options = null,
   CancellationToken cancellationToken = default)
   where TTarget : class
   where TSource : class;

// PostgreSQL — same shape, options: NpgsqlBulkInsertFromQueryOptions?
// SQLite     — same shape, options: SqliteBulkInsertFromQueryOptions?
```

The `InsertPropertyBuilder<TTarget, TSource>` maps each target column to a source
value expression:

```csharp
InsertPropertyBuilder<TTarget, TSource> Map<TProp>(
   Expression<Func<TTarget, TProp>> targetColumnSelector,
   Expression<Func<TSource, TProp>> sourceValueSelector);
```

Options (`SqlServerBulkInsertFromQueryOptions` / `NpgsqlBulkInsertFromQueryOptions` /
`SqliteBulkInsertFromQueryOptions`) expose only target-table overrides:

| Property | Purpose |
|----------|---------|
| `string? TableName` | Override the target table name (else from the model) |
| `string? Schema` | Override the target schema (else from the model) |

## Examples

### Simplest — insert a projection of another query

Identical across SQL Server / PostgreSQL / SQLite (only identifier quoting in the
generated SQL differs):

```csharp
using Thinktecture;

// Source: any server-side IQueryable<TSource>.
var sourceQuery = ctx.Products
   .Where(p => p.Discontinued)
   .Select(p => new { p.Id, p.Name, p.Price });

int affected = await ctx.Set<Product>().BulkInsertAsync(
   sourceQuery,
   builder => builder
      .Map(target => target.Id,    source => source.Id)
      .Map(target => target.Name,  source => source.Name)
      .Map(target => target.Price, source => source.Price));
```

Generates `INSERT INTO [Products] (...) SELECT s.[Id], s.[Name], s.[Price] FROM (<sourceQuery>) AS s`.

### Source can be a temp table

`tempTable.Query` (an `IQueryable<T>`) is a valid source — register the row type
via `ConfigureTempTableEntity<T>()` if it is temp-table-only (see `Temp-Tables.md`):

```csharp
await using var tempTable = await ctx.BulkInsertIntoTempTableAsync(rows);

await ctx.Set<Product>().BulkInsertAsync(
   tempTable.Query,
   b => b.Map(t => t.Id, s => s.Id)
         .Map(t => t.Name, s => s.Name));
```

### Value selectors are full expressions

The source side of `Map` is not limited to a plain property — it accepts:

```csharp
await ctx.Set<Product>().BulkInsertAsync(
   sourceQuery,
   b => b
      .Map(t => t.Id,    s => s.Id)
      .Map(t => t.Name,  s => s.Name!.ToUpper())          // server function
      .Map(t => t.Count, s => s.Count * 2 + 1)            // arithmetic on source
      .Map(t => t.Note,  s => myCapturedVariable)         // captured variable → parameter
      .Map(t => t.Status, s => ProductStatus.Published)   // constant / static member
      .Map(t => EF.Property<int>(t, "_privateField"),
           s => EF.Property<int>(s, "_privateField")));   // shadow / backing field
```

- Simple property access and `EF.Property<T>(...)` map to source columns.
- Constants, captured variables and static members (e.g. smart-enum values) are
  emitted as SQL parameters (`@__bv_N`).
- Arithmetic / method-call expressions are translated to SQL
  (`@__ev_N` for any embedded constant values).

### Override the target table

```csharp
await ctx.Set<Product>().BulkInsertAsync(
   sourceQuery,
   b => b.Map(t => t.Id, s => s.Id).Map(t => t.Name, s => s.Name),
   new SqlServerBulkInsertFromQueryOptions { Schema = "archive", TableName = "ArchivedProducts" });
   // PostgreSQL: new NpgsqlBulkInsertFromQueryOptions { ... }
   // SQLite:     new SqliteBulkInsertFromQueryOptions { ... }
```

## Notes & pitfalls

- **Enable the feature** (`AddBulkOperationSupport()`), or the method throws at
  runtime.
- This is **`DbSet<TTarget>.BulkInsertAsync(IQueryable source, map)`** — a
  *different* API from `DbContext.BulkInsertAsync(collection)` (the in-memory
  variant, see `Bulk-Insert.md`). Calling `ctx.Set<T>()` first gives you the
  `DbSet`.
- You must configure **at least one** `Map` entry, otherwise it throws
  `ArgumentException`.
- The source must be a server-evaluable `IQueryable<TSource>`; its SQL and
  parameters are inlined as the `SELECT` subquery.
- Returns `Task<int>` — the number of inserted rows.
- For a temp-table source whose row type is registered only via
  `ConfigureTempTableEntity<T>()`, column names resolve correctly because the
  executor also looks the type up under its temp-table name.
