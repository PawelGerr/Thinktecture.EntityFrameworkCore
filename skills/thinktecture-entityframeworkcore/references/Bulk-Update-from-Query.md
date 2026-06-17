# Bulk Update (from a query)

Update a table by joining it to the result of a server-side LINQ query, without a
round-trip to the app. **SQL Server** generates
`UPDATE t SET … FROM target INNER JOIN (subquery) AS s ON …`; **PostgreSQL** and
**SQLite** generate `UPDATE target AS t SET … FROM (subquery) AS s WHERE …`.
Returns the number of affected rows.

> This page is for updating from another query/temp table. To update a
> collection you already hold in memory (`List<T>`, array, …), see
> `Bulk-Update.md`. For a *single-table* set-based update with a simple `Where`,
> prefer EF Core's native `ExecuteUpdateAsync` — reach for this API when the
> update must **join another query** (set target columns from a related source
> set) or needs **composite-key** joins.

**Enable:** `AddBulkOperationSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;`

## API

The method is on **`DbSet<TTarget>`** (not `DbContext`). The signature is
identical for every provider — only the `options` type differs:

```csharp
// SQL Server
Task<int> BulkUpdateAsync<TTarget, TSource, TResult>(
   this DbSet<TTarget> target,
   IQueryable<TSource> sourceQuery,
   Expression<Func<TTarget, TResult?>> targetKeySelector,
   Expression<Func<TSource, TResult?>> sourceKeySelector,
   Func<SetPropertyBuilder<TTarget, TSource>, SetPropertyBuilder<TTarget, TSource>> setPropertyCalls,
   Expression<Func<TTarget, TSource, bool>>? filter = null,
   SqlServerBulkUpdateFromQueryOptions? options = null,
   CancellationToken cancellationToken = default)
   where TTarget : class
   where TSource : class;

// PostgreSQL — same shape, options: NpgsqlBulkUpdateFromQueryOptions?
// SQLite     — same shape, options: SqliteBulkUpdateFromQueryOptions?
```

The `SetPropertyBuilder<TTarget, TSource>` assigns each target property a value
expression (which may reference **both** target `e` and source `f`):

```csharp
SetPropertyBuilder<TTarget, TSource> Set<TProp>(
   Expression<Func<TTarget, TProp>> targetPropertySelector,
   Expression<Func<TTarget, TSource, TProp>> valueSelector);
```

Options (`SqlServerBulkUpdateFromQueryOptions` / `NpgsqlBulkUpdateFromQueryOptions` /
`SqliteBulkUpdateFromQueryOptions`) expose only target-table overrides:

| Property | Purpose |
|----------|---------|
| `string? TableName` | Override the target table name (else from the model) |
| `string? Schema` | Override the target schema (else from the model) |

## Examples

### Simplest — single-key join, copy source values

Identical across SQL Server / PostgreSQL / SQLite (only the generated SQL shape /
quoting differs):

```csharp
using Thinktecture;

var sourceQuery = ctx.Set<ProductUpdateDto>(); // any server-side IQueryable<TSource>

int affected = await ctx.Set<Product>().BulkUpdateAsync(
   sourceQuery,
   target => target.Id,     // join key on target
   source => source.Id,     // join key on source
   builder => builder
      .Set(e => e.Name,  (e, f) => f.Name)
      .Set(e => e.Count, (e, f) => f.Count));
```

### Composite key (anonymous type)

Key selectors support single **and** composite keys; build a composite key with
an anonymous type — the member names must match on both sides:

```csharp
await ctx.Set<Product>().BulkUpdateAsync(
   sourceQuery,
   e => new { e.Id, e.Name },
   f => new { f.Id, f.Name },
   builder => builder.Set(e => e.Count, (e, f) => f.Count));
```

The source key may map differently named columns, since you control the projection:

```csharp
var sourceQuery = ctx.Products.Select(p => new { Identifier = p.Id, Number = p.Count });

await ctx.Set<Product>().BulkUpdateAsync(
   sourceQuery,
   e => e.Id,
   f => f.Identifier,
   builder => builder.Set(e => e.Count, (e, f) => f.Number));
```

### Optional `filter` — restrict which rows update

The `filter` is `Expression<Func<TTarget, TSource, bool>>` and can reference
**both** target and source. Rows that match the join but fail the filter are left
untouched (it adds a `WHERE` / extra `ON` condition):

```csharp
await ctx.Set<Product>().BulkUpdateAsync(
   sourceQuery,
   e => e.Id,
   f => f.Id,
   builder => builder.Set(e => e.Name, (e, f) => f.Name),
   filter: (e, f) => e.Count < f.Count);        // target & source compared

// also valid: filter referencing only the target, a captured variable,
// or arithmetic across both, e.g. (e, f) => e.Count + f.Count > 15
```

### Value expressions are full expressions

The `Set` value selector accepts more than plain source properties:

```csharp
await ctx.Set<Product>().BulkUpdateAsync(
   sourceQuery,
   e => e.Id,
   f => f.Id,
   builder => builder
      .Set(e => e.Name,   (e, f) => f.Name)                  // source property
      .Set(e => e.Count,  (e, f) => e.Count + f.Count * 2)   // arithmetic over target & source
      .Set(e => e.Total,  (e, f) => 42)                      // constant → parameter
      .Set(e => e.Note,   (e, f) => myCapturedVariable)      // captured variable
      .Set(e => e.Status, (e, f) => ProductStatus.Published) // static member / smart-enum
      .Set(e => EF.Property<int>(e, "_privateField"),
           (e, f) => EF.Property<int>(f, "_privateField"))); // shadow / backing field
```

- Simple property access and `EF.Property<T>(...)` map to columns.
- Constants, captured variables and static members are emitted as SQL parameters
  (`@__bv_N`); value-converter-typed values (e.g. smart enums) are handled too.
- Arithmetic / method-call expressions are translated to SQL (`@__ev_N` for any
  embedded constant values).

### Source can be a temp table

`tempTable.Query` is a valid source. For a temp-table-only row type, register it
via `ConfigureTempTableEntity<T>()` (see `Temp-Tables.md`) so its column names
resolve correctly:

```csharp
await using var tempTable = await ctx.BulkInsertIntoTempTableAsync(rows);

await ctx.Set<Product>().BulkUpdateAsync(
   tempTable.Query,
   e => e.Id,
   f => f.Id,
   b => b.Set(e => e.Count, (e, f) => e.Count + f.Count));
```

### Override the target table

```csharp
await ctx.Set<Product>().BulkUpdateAsync(
   sourceQuery, e => e.Id, f => f.Id,
   b => b.Set(e => e.Name, (e, f) => f.Name),
   options: new SqlServerBulkUpdateFromQueryOptions { Schema = "dbo", TableName = "Products_Staging" });
   // PostgreSQL: new NpgsqlBulkUpdateFromQueryOptions { ... }
   // SQLite:     new SqliteBulkUpdateFromQueryOptions { ... }
```

## Notes & pitfalls

- **Enable the feature** (`AddBulkOperationSupport()`), or the method throws at
  runtime.
- This is **`DbSet<TTarget>.BulkUpdateAsync(IQueryable source, …)`** — a
  *different* API from `DbContext.BulkUpdateAsync(collection)` (the in-memory
  variant, see `Bulk-Update.md`). Call `ctx.Set<T>()` first to get the `DbSet`.
- You must configure **at least one** `Set` assignment, otherwise it throws
  `ArgumentException`.
- **When to use this vs alternatives:** join another query / composite-key join →
  this API; single-table simple `Where` → EF Core native `ExecuteUpdateAsync`;
  a collection in memory → `Bulk-Update.md`.
- The `filter` and the `Set` value expressions share the same translator, so
  parameters across both are unified (`@__ev_N`).
- SQL Server emits `UPDATE t SET … FROM target INNER JOIN (subquery) AS s ON …`;
  PostgreSQL/SQLite emit `UPDATE target AS t SET … FROM (subquery) AS s WHERE …`.
- Returns `Task<int>` — the number of updated rows (`0` if the filter excludes
  everything).
