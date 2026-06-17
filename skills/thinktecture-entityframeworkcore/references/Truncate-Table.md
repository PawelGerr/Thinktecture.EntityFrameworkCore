# Truncate Table

Remove **all** rows from a table in one dedicated operation. On SQL Server and
PostgreSQL this issues `TRUNCATE TABLE` (a minimally-logged metadata operation
that also **resets the identity/auto-increment seed**); on SQLite — which has no
`TRUNCATE` — it issues a single `DELETE FROM <table>`.

> Prefer this over EF Core's `ExecuteDeleteAsync()` when you want to clear the
> **entire** table: `TRUNCATE` is dramatically faster (it deallocates pages
> instead of logging per-row deletes) and resets the identity seed.
> `ExecuteDeleteAsync()` is the right tool only when you need a **conditional**
> delete (a `Where` filter) — `TruncateTableAsync` takes no predicate.

**Enable:** `AddBulkOperationSupport()` in `UseSqlServer/UseNpgsql/UseSqlite`.
**Namespace:** `using Thinktecture;`

## API

The provider-agnostic methods are on `DbContext`:

```csharp
Task TruncateTableAsync<T>(
   this DbContext ctx,
   CancellationToken cancellationToken = default) where T : class;

Task TruncateTableAsync(
   this DbContext ctx,
   Type type,
   CancellationToken cancellationToken = default);
```

PostgreSQL adds two extra overloads with a `cascade` flag (in the same
`Thinktecture` namespace, via `NpgsqlBulkOperationsDbContextExtensions`):

```csharp
Task TruncateTableAsync<T>(
   this DbContext ctx,
   bool cascade,
   CancellationToken cancellationToken = default) where T : class;

Task TruncateTableAsync(
   this DbContext ctx,
   Type type,
   bool cascade,
   CancellationToken cancellationToken = default);
```

## Examples

### Clear a table

```csharp
using Thinktecture;

await ctx.TruncateTableAsync<Product>();
```

SQL Server / PostgreSQL:

```sql
TRUNCATE TABLE [dbo].[Products];
```

SQLite (no `TRUNCATE` in SQLite):

```sql
DELETE FROM "Products";
```

### Truncate by runtime `Type`

```csharp
await ctx.TruncateTableAsync(typeof(Product));
```

### PostgreSQL — `CASCADE`

PostgreSQL refuses to truncate a table that has foreign-key references from
other tables unless `CASCADE` is given. Pass `cascade: true` to also truncate
the dependent tables:

```csharp
await ctx.TruncateTableAsync<Product>(cascade: true);
```

```sql
TRUNCATE TABLE "Products" CASCADE;
```

## Notes & pitfalls

- **Enable the feature** (`AddBulkOperationSupport()`), or the method throws at
  runtime.
- **No filter.** `TruncateTableAsync` always clears the whole table. For a
  conditional delete use EF Core's native `ExecuteDeleteAsync()` with a `Where`.
- **Resets identity/auto-increment** (SQL Server & PostgreSQL `TRUNCATE`). On
  SQLite the operation is a `DELETE`, so it is *not* minimally logged and does
  not reset `AUTOINCREMENT` the same way — don't rely on identity reset there.
- **Foreign keys:** SQL Server `TRUNCATE` fails if the table is referenced by a
  foreign key. PostgreSQL needs `cascade: true` to truncate referenced tables.
  SQLite (via `DELETE`) still enforces FK constraints if enabled.
- Truncating an already-empty table is a no-op and does not throw.
