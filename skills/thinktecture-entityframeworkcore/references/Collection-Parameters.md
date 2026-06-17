# Collection Parameters ("temp tables light")

Turn an in-memory collection into an `IQueryable<T>` that the database unfolds
into a rowset — pass a `List<Guid>`, `int[]`, or a list of small objects into a
query and `JOIN` / `Contains` against it. The collection is serialized to **one
JSON parameter**; there is **no `CREATE TABLE`, no bulk insert, and no disposal**.

> **vs Temp Tables** — Temp tables give you a real, indexable, multi-column,
> primary-keyed table (DDL + bulk insert + `await using` cleanup). Collection
> parameters are lighter: a single query parameter, nothing to dispose. Prefer
> collection parameters for read-side `IN` / a single `JOIN`; reach for
> **Temp Tables** (`Temp-Tables.md`) when you need indexes, a PK, or to reuse a
> set across many queries.
>
> **vs EF Core's native `.Where(e => ids.Contains(e.Id))`** — native `Contains`
> inlines each value as its own SQL parameter, so large lists blow up the query
> plan cache and can hit parameter limits. A collection parameter is a *single*
> parameter regardless of list size, giving stable plans for large or reused
> lists.

**SQL Server + PostgreSQL only.** SQLite is **not** supported — SQLite consumers
should use **Temp Tables** (`Temp-Tables.md`) instead. SQL Server requires JSON
support (SQL Server 2016+ / Azure SQL).

**Enable:** `AddCollectionParameterSupport()` (on the `UseSqlServer` / `UseNpgsql`
sub-builder).
**Namespace:** `using Thinktecture;`

## Enabling support

```csharp
using Thinktecture;

services.AddDbContext<MyDbContext>(builder => builder
   // SQL Server
   .UseSqlServer(connectionString, sql => sql.AddCollectionParameterSupport())
   // PostgreSQL
   // .UseNpgsql(connectionString, npgsql => npgsql.AddCollectionParameterSupport())
);
```

Both providers expose the same signature:

```csharp
// SQL Server
SqlServerDbContextOptionsBuilder AddCollectionParameterSupport(
   this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
   JsonSerializerOptions? jsonSerializerOptions = null,
   bool addCollectionParameterSupport = true,
   bool configureCollectionParametersForPrimitiveTypes = true,
   bool useDeferredSerialization = false);

// PostgreSQL
NpgsqlDbContextOptionsBuilder AddCollectionParameterSupport(
   this NpgsqlDbContextOptionsBuilder npgsqlOptionsBuilder,
   JsonSerializerOptions? jsonSerializerOptions = null,
   bool addCollectionParameterSupport = true,
   bool configureCollectionParametersForPrimitiveTypes = true,
   bool useDeferredSerialization = false);
```

- `jsonSerializerOptions` — pass your own `System.Text.Json` options (custom
  converters, naming policy) to control how values/objects are serialized into
  the JSON parameter.
- `configureCollectionParametersForPrimitiveTypes` (default `true`) —
  auto-registers scalar parameters for common primitives (`int`, `long`, `Guid`,
  `bool`, `decimal`, `string`, `DateTime`, …), so you usually don't need to
  register them yourself.
- `useDeferredSerialization` (default `false`) — when `false`, the collection is
  serialized eagerly at the moment `CreateScalar/ComplexCollectionParameter` is
  called; when `true`, serialization is deferred until the query actually
  executes.

## Step 0 (sometimes required): register the parameter type in `OnModelCreating`

Primitive scalar types are registered automatically (see flag above). You must
register explicitly for:

- **complex/object collections** — always (`ConfigureComplexCollectionParameter<T>`),
- **scalar types that need configuration** (precision, value converters, or
  custom types not covered by the primitive defaults).

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
   base.OnModelCreating(modelBuilder);

   // Scalar with extra configuration:
   modelBuilder.ConfigureScalarCollectionParameter<decimal>(builder =>
      builder.Property(e => e.Value).HasPrecision(10, 5));

   // Complex object (always required) — property names must match JSON:
   modelBuilder.ConfigureComplexCollectionParameter<ProductFilter>();
}
```

## API

```csharp
// Scalar collection (list of primitives) → IQueryable<T>.
IQueryable<T> CreateScalarCollectionParameter<T>(
   this DbContext ctx, IReadOnlyCollection<T> values, bool applyDistinct = true);

// Complex collection (list of objects) → IQueryable<T>.
IQueryable<T> CreateComplexCollectionParameter<T>(
   this DbContext ctx, IReadOnlyCollection<T> objects, bool applyDistinct = true)
   where T : class;
```

`applyDistinct` defaults to `true` — it adds `DISTINCT` over the unfolded rowset.
**Keep it `true`** unless you need duplicates: it yields markedly better
execution plans.

Model-builder registration:

```csharp
void ConfigureScalarCollectionParameter<T>(
   this ModelBuilder modelBuilder,
   Action<EntityTypeBuilder<ScalarCollectionParameter<T>>>? buildAction = null);

void ConfigureComplexCollectionParameter<T>(
   this ModelBuilder modelBuilder,
   Action<EntityTypeBuilder<T>>? buildAction = null) where T : class;
```

Key types:

| Type | Purpose |
|------|---------|
| `ScalarCollectionParameter<T>` | `record ScalarCollectionParameter<T>(T Value)` — the keyless entity backing a scalar parameter; you query the `T` directly, not this wrapper |
| `JsonCollectionParameter` (SQL Server) / `NpgsqlJsonCollectionParameter` (PostgreSQL) | internal: the JSON-serialized parameter sent to the DB (`OPENJSON` on SQL Server, JSON functions on PostgreSQL). Not used directly by consumers |

## Examples

### SQL Server — `WHERE Id IN (large list)`

```csharp
using Thinktecture;

List<Guid> productIds = GetIds(); // potentially thousands

IQueryable<Guid> idsParam = ctx.CreateScalarCollectionParameter(productIds);

var products = await ctx.Products
   .Where(p => idsParam.Contains(p.Id))
   .ToListAsync();
// → single JSON parameter unfolded via OPENJSON, not N inlined parameters
```

### SQL Server — `JOIN` against a list of objects

```csharp
using Thinktecture;

// Register once in OnModelCreating:
//   modelBuilder.ConfigureComplexCollectionParameter<ProductFilter>();
public record ProductFilter(Guid Id, int MinCount);

var filters = new[]
{
   new ProductFilter(id1, 10),
   new ProductFilter(id2, 50),
};

IQueryable<ProductFilter> filterParam = ctx.CreateComplexCollectionParameter(filters);

var matches = await ctx.Products
   .Join(filterParam,
         p => p.Id,
         f => f.Id,
         (p, f) => new { p.Name, f.MinCount })
   .Where(x => x.MinCount > 0)
   .ToListAsync();
```

### PostgreSQL — identical consumer code

The call shape is the same; only the enabling line differs.

```csharp
using Thinktecture;

// builder.UseNpgsql(conn, npgsql => npgsql.AddCollectionParameterSupport());

List<int> productIds = GetIds();

IQueryable<int> idsParam = ctx.CreateScalarCollectionParameter(productIds);

var products = await ctx.Products
   .Where(p => idsParam.Contains(p.Id))
   .ToListAsync();
```

```csharp
// Complex collection JOIN on PostgreSQL — same API as SQL Server.
var filterParam = ctx.CreateComplexCollectionParameter(filters);

var matches = await ctx.Products
   .Join(filterParam, p => p.Id, f => f.Id, (p, f) => new { p.Name, f.MinCount })
   .ToListAsync();
```

### Custom JSON serialization

Property names of the .NET type must match the JSON property names. With default
`System.Text.Json` they line up automatically; otherwise use
`[JsonPropertyName]` or pass `JsonSerializerOptions` to
`AddCollectionParameterSupport`.

```csharp
public record ProductFilter(
   [property: JsonPropertyName("Id")] Guid Id,
   int MinCount);

// or globally:
var jsonOptions = new JsonSerializerOptions { /* converters, naming policy */ };
builder.UseSqlServer(conn, sql => sql.AddCollectionParameterSupport(jsonOptions));
```

## Notes & pitfalls

- **SQL Server + PostgreSQL only.** No SQLite support — use **Temp Tables** there.
- **Keep `applyDistinct: true`** (the default). It produces far better execution
  plans; only set `false` when you genuinely need duplicate rows.
- **Complex types must be registered** via `ConfigureComplexCollectionParameter<T>()`
  in `OnModelCreating`, and the type must be a reference type (`class`/`record`).
  Forgetting this throws at the call site.
- **Pass `IReadOnlyCollection<T>`** — `List<T>`, arrays, etc. work; a lazy
  `IEnumerable<T>` does not match the signature.
- **JSON property names must match** the .NET property names. Mismatches yield
  `null`/default columns silently; fix with `[JsonPropertyName]` or custom
  `JsonSerializerOptions`.
- **Reuse the returned `IQueryable<T>`** freely within the same `DbContext`; it's
  just a query, there's nothing to dispose (unlike a temp table reference).
- Behind the scenes: the collection becomes one JSON parameter, expanded by
  `OPENJSON` (SQL Server) or PostgreSQL's JSON functions — hence the single,
  cache-friendly parameter regardless of list size.
