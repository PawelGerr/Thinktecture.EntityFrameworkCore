# Change the default schema at runtime

Decide the **default database schema** of a `DbContext` (and its migrations)
**per instance, at runtime**, instead of hard-coding it in `OnModelCreating`.
The same `DbContext` type can target `tenant_a`, `tenant_b`, … by simply
constructing it with a different schema value. Works on **SQL Server**,
**PostgreSQL** and **SQLite**.

> Use this for schema-per-tenant designs, or any case where the schema is not
> known at compile time. EF Core normally bakes the schema into the cached
> model; this feature makes the schema part of the model cache key so each
> schema gets its own correctly-built model — handled for you.

**Enable:** `AddSchemaRespectingComponents()` on the **outer**
`DbContextOptionsBuilder` (provider-agnostic — *not* inside
`UseSqlServer`/`UseNpgsql`/`UseSqlite`).
**Namespace:** `using Thinktecture;` (the `IDbDefaultSchema` interface lives in
`Thinktecture.EntityFrameworkCore`).

## API

Two pieces are required: implement an interface on your `DbContext`, and
register the schema-respecting components.

```csharp
// 1. Interface your DbContext implements (namespace Thinktecture.EntityFrameworkCore)
public interface IDbDefaultSchema
{
   string? Schema { get; }
}

// 2. Registration extension (generic + non-generic), provider-agnostic
DbContextOptionsBuilder<T> AddSchemaRespectingComponents<T>(
   this DbContextOptionsBuilder<T> builder,
   bool addDefaultSchemaRespectingComponents = true) where T : DbContext;

DbContextOptionsBuilder AddSchemaRespectingComponents(
   this DbContextOptionsBuilder builder,
   bool addDefaultSchemaRespectingComponents = true);
```

There is also a ready-made `IDbDefaultSchema` implementation if you want to pass
one around (e.g. inject it):

```csharp
// namespace Thinktecture.EntityFrameworkCore
public sealed class DbDefaultSchema : IDbDefaultSchema
{
   public string Schema { get; }
   public DbDefaultSchema(string schema);   // throws ArgumentNullException if null
}
```

## Examples

### 1. Implement `IDbDefaultSchema` on your `DbContext`

The `Schema` property is the default schema. How you populate it is up to you —
a constructor parameter is the common choice.

```csharp
using Thinktecture.EntityFrameworkCore;

public class MyDbContext : DbContext, IDbDefaultSchema
{
   public string? Schema { get; }

   public DbSet<Product> Products { get; set; }

   public MyDbContext(DbContextOptions<MyDbContext> options, string? schema)
      : base(options)
   {
      Schema = schema;
   }
}
```

You do **not** call `modelBuilder.HasDefaultSchema(...)`. The registered
components apply `Schema` to the model for you (and skip applying it where the
entity already pins an explicit schema).

### 2. Register the components

```csharp
using Thinktecture;

services.AddDbContext<MyDbContext>(builder => builder
   .UseSqlServer(connectionString)        // or .UseNpgsql(...) / .UseSqlite(...)
   .AddSchemaRespectingComponents());      // outer builder, provider-agnostic
```

### 3. Construct with different schemas at runtime

```csharp
// Same DbContext type, two different schemas — each gets its own cached model.
var ctxA = new MyDbContext(options, "tenant_a");
var ctxB = new MyDbContext(options, "tenant_b");

await ctxA.Products.ToListAsync(); // SELECT ... FROM [tenant_a].[Products]
await ctxB.Products.ToListAsync(); // SELECT ... FROM [tenant_b].[Products]
```

If you register the context in DI, supply the schema where you build it — e.g.
via a factory that reads the current tenant:

```csharp
services.AddDbContext<MyDbContext>(
   (sp, builder) => builder
      .UseSqlServer(connectionString)
      .AddSchemaRespectingComponents());

// resolve the schema per request and pass it into the constructor
// (e.g. with AddDbContextFactory / a custom factory delegate).
```

### 4. (Optional) Use the schema inside a migration

An `IDbDefaultSchema` can be injected into a migration when raw SQL needs to
know the schema:

```csharp
public partial class MyMigration : Migration
{
   public MyMigration(IDbDefaultSchema schema) { /* use schema.Schema */ }
}
```

### 5. (Optional) Migrations history table schema

`IDbDefaultSchema` does **not** move the `__EFMigrationsHistory` table. Use
EF Core's own option for that:

```csharp
.UseSqlServer(connectionString, sql =>
{
   if (schema is not null)
      sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
})
// PostgreSQL: .UseNpgsql(..., npgsql => npgsql.MigrationsHistoryTable(...))
```

## Notes & pitfalls

- **Enable on the outer builder.** `AddSchemaRespectingComponents()` is
  provider-agnostic; call it on the `DbContextOptionsBuilder`, not the
  `UseXxx(...)` sub-builder.
- **Your `DbContext` must implement `IDbDefaultSchema`.** Registering the
  components without the interface has no effect; and a migration that requires
  the schema throws if its context type doesn't implement the interface.
- **Don't also call `HasDefaultSchema(...)`** in `OnModelCreating` — let the
  registered `DefaultSchemaModelCustomizer<T>` apply `Schema`. Entities with an
  explicitly configured schema keep it.
- **Model caching is handled.** The schema is folded into the model cache key
  (`DefaultSchemaRespectingModelCacheKeyFactory<T>`), so two instances with
  different `Schema` values get two correctly-built, separately-cached models —
  you don't need to do anything for this to work.
- **Migrations history table is separate** — `IDbDefaultSchema` does not affect
  it; use EF Core's `MigrationsHistoryTable(name, schema)` if you need to move
  it.
- A `null` `Schema` means "use the provider's default schema" (e.g. `dbo` on SQL
  Server, `public` on PostgreSQL).
