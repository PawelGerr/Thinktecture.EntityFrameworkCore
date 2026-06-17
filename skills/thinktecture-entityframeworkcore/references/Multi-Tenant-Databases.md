# Multi-Tenant Databases (cross-database queries)

Run the same LINQ queries against a **per-tenant database** without changing the
queries. You map a tenant to a database name, and the library rewrites every
table reference in the generated SQL to `[database].[schema].[table]`
(or `[database]..[table]` when there is no schema), so reads target the tenant's
database.

> **SQL Server only.** There is no PostgreSQL or SQLite equivalent.

> This affects **query** SQL generation (the table prefix in `SELECT`/`JOIN`/
> `INCLUDE`/views). It does not change the physical connection — all targeted
> databases must be reachable through the one connection the `DbContext` uses
> (e.g. on the same SQL Server instance).

**Enable:** `AddTenantDatabaseSupport<TFactory>()` in `UseSqlServer`.
**Namespace:** `using Thinktecture;` (extension) /
`using Thinktecture.EntityFrameworkCore.Query;` (the interfaces you implement).

## API

### Extension method (registration)

```csharp
SqlServerDbContextOptionsBuilder AddTenantDatabaseSupport<TTenantDatabaseProviderFactory>(
   this SqlServerDbContextOptionsBuilder builder,
   bool addTenantSupport = true,
   ServiceLifetime databaseProviderLifetime = ServiceLifetime.Singleton)
   where TTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory;
```

### Interfaces you implement

```csharp
namespace Thinktecture.EntityFrameworkCore.Query;

public interface ITenantDatabaseProviderFactory
{
   ITenantDatabaseProvider Create();
}

public interface ITenantDatabaseProvider
{
   // The current tenant (for your own logic; the library does not read it directly).
   string? Tenant { get; }

   // Return the database name for a given table; return null/empty to leave the
   // table unprefixed (i.e. use the connection's default database).
   string? GetDatabaseName(string? schema, string table);
}
```

- The **factory** is resolved with the lifetime you pass to
  `databaseProviderLifetime` (default `Singleton`); its `Create()` is called per
  query to produce an `ITenantDatabaseProvider`.
- For every table in a query the SQL generator calls
  `GetDatabaseName(schema, table)`. A non-empty result is emitted as a database
  prefix; `null`/empty leaves the table as-is.

## Examples

### 1. Implement the provider + factory

```csharp
using Thinktecture.EntityFrameworkCore.Query;

// One provider instance services one query. Resolve the current tenant from
// wherever your app keeps it (HTTP context, ambient scope, DI, …).
public sealed class TenantDatabaseProvider : ITenantDatabaseProvider
{
   public string? Tenant { get; }

   public TenantDatabaseProvider(string? tenant) => Tenant = tenant;

   public string? GetDatabaseName(string? schema, string table)
   {
      // Map tenant -> database name. Must be stable for a given tenant.
      return Tenant switch
      {
         "acme"   => "Acme_Db",
         "globex" => "Globex_Db",
         _        => null,   // null/empty => no prefix, use the default database
      };
   }
}

// Registered as a singleton by default, so it must be thread-safe and stateless.
public sealed class TenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
{
   private readonly ICurrentTenant _currentTenant; // your own abstraction

   public TenantDatabaseProviderFactory(ICurrentTenant currentTenant)
      => _currentTenant = currentTenant;

   public ITenantDatabaseProvider Create()
      => new TenantDatabaseProvider(_currentTenant.TenantId);
}
```

### 2. Register it

```csharp
using Thinktecture;

services.AddDbContext<MyDbContext>(builder => builder
   .UseSqlServer(connectionString, sql => sql
      .AddTenantDatabaseSupport<TenantDatabaseProviderFactory>()));
```

### 3. Query — resolution is automatic

```csharp
// No tenant-specific code in the query itself.
var products = await ctx.Products
                        .Include(p => p.Category)
                        .ToListAsync();
```

For tenant `"acme"` (mapping `Products`/`Categories` → `Acme_Db`), the generated
SQL targets that database:

```sql
SELECT ...
FROM [Acme_Db].[dbo].[Products] AS [p]
LEFT JOIN [Acme_Db].[dbo].[Categories] AS [c] ON ...
```

When `GetDatabaseName` returns `null`/empty, the table is left unprefixed and the
query runs against the connection's default database:

```sql
SELECT ... FROM [dbo].[Products] AS [p]
```

If a table has no schema, the prefix uses the `db..table` form, e.g.
`[Acme_Db]..[Products]`.

## Notes & pitfalls

- **SQL Server only** — there is no PostgreSQL/SQLite support.
- **Database names must be stable per tenant.** Returning different database
  names for the same tenant breaks EF Core's query/plan caching assumptions —
  the docs explicitly require the name not to change for a given tenant.
- The **factory must be safe as a singleton** (the default lifetime). Keep it
  stateless; read the current tenant through an injected abstraction rather than
  storing it on the factory.
- The prefix is applied to **tables and views** in `FROM`, `JOIN`, and
  `Include`-generated joins — all table references in the query get the same
  treatment.
- This rewrites **query SQL** only. It does not redirect bulk operations,
  migrations, or the physical connection; every targeted database must be
  reachable via the single connection the `DbContext` already uses.
- Return `null` or empty from `GetDatabaseName` to opt a specific table out of
  prefixing (falls back to the default database).