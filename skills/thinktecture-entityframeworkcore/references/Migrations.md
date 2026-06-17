# Migration helpers

Migration-time SQL helpers that EF Core doesn't expose: conditional
`IF [NOT] EXISTS` checks, identity columns, covering-index `INCLUDE` columns,
and (non-)clustered primary keys. They are fluent extension methods on the
`OperationBuilder<T>` returned by `MigrationBuilder` operations, applied inside a
migration's `Up()` / `Down()`.

> The helpers only take effect when the **Thinktecture migrations SQL generator**
> is registered (see Enable). Without it, the annotations are ignored — the
> migration runs as a plain EF Core migration.

**Enable:**
`UseThinktectureSqlServerMigrationsSqlGenerator()` inside `UseSqlServer(...)`
(SQL Server) /
`UseThinktectureNpgsqlMigrationsSqlGenerator()` inside `UseNpgsql(...)` (PostgreSQL).
**Not available for SQLite.**
**Namespace:** `using Thinktecture;`

```csharp
// SQL Server
builder.UseSqlServer(conn, sql => sql.UseThinktectureSqlServerMigrationsSqlGenerator());
// PostgreSQL
builder.UseNpgsql(conn, npg => npg.UseThinktectureNpgsqlMigrationsSqlGenerator());
```

## API

| Helper | Method | Applies to | SQL Server | PostgreSQL |
|--------|--------|-----------|:---:|:---:|
| Run only if (not) exists | `IfNotExists<T>()` / `IfExists<T>()` | `OperationBuilder<T>` (table/column/index/unique-constraint ops) | ✔ | ✔ |
| Identity column | `AsIdentityColumn()` | `OperationBuilder<AddColumnOperation>` | ✔ | ✔ |
| Covering-index include columns | `IncludeColumns(params string[])` | `OperationBuilder<CreateIndexOperation>` | ✔ | ✔ |
| (Non-)clustered PK | `IsClustered(bool = true)` | `OperationBuilder<AddPrimaryKeyOperation>` | ✔ | — |

All four live in the root `Thinktecture` namespace
(`SqlServerOperationBuilderExtensions` / `NpgsqlOperationBuilderExtensions`).

## Examples

### `IfNotExists` / `IfExists` — conditional create/drop

Flags an operation so it only runs when the target object does (not) exist.
`IfNotExists()` pairs with create operations; `IfExists()` pairs with drop
operations — mixing them up throws at generation time.

```csharp
using Thinktecture;

public partial class AddOrderItemsIndex : Migration
{
   protected override void Up(MigrationBuilder migrationBuilder)
   {
      migrationBuilder
         .CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId")
         .IfNotExists();
   }

   protected override void Down(MigrationBuilder migrationBuilder)
   {
      migrationBuilder
         .DropIndex("IX_OrderItems_ProductId", "OrderItems")
         .IfExists();
   }
}
```

Supported operations (same for both providers):

| Operation | Check |
|-----------|-------|
| `CreateTable` / `DropTable` | `IfNotExists` / `IfExists` |
| `AddColumn` / `DropColumn` | `IfNotExists` / `IfExists` |
| `CreateIndex` / `DropIndex` | `IfNotExists` / `IfExists` |
| `AddUniqueConstraint` / `DropUniqueConstraint` | `IfNotExists` / `IfExists` |

PostgreSQL emits native `IF [NOT] EXISTS` syntax; SQL Server wraps the statement
in an existence check.

### `AsIdentityColumn` — auto-increment column (both providers)

```csharp
migrationBuilder.AddColumn<int>("Id", "Customer")
                .AsIdentityColumn();
```

SQL Server maps to `IDENTITY`; PostgreSQL maps to `GENERATED ALWAYS AS IDENTITY`.

### `IncludeColumns` — covering index (both providers)

Adds non-key `INCLUDE` columns to an index so the index covers more queries
without bloating the key.

```csharp
migrationBuilder.CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId")
                .IncludeColumns("OrderId", "Count");
```

At least one column must be supplied (empty array throws `ArgumentException`).

### `IsClustered` — (non-)clustered primary key (SQL Server only)

```csharp
migrationBuilder.AddPrimaryKey("PK_Customers", "Customers", "Id")
                .IsClustered(true);   // pass false for a NONCLUSTERED PK
```

`true` (the default) produces a clustered PK; `false` a nonclustered PK.
There is **no PostgreSQL equivalent** of this helper.

## Notes & pitfalls

- **Register the generator**, or every helper here is silently ignored — the
  migration runs as a plain EF Core migration.
- **SQLite is not supported** — neither `UseThinktecture…MigrationsSqlGenerator`
  variant exists for SQLite.
- **Check direction matters**: `IfNotExists()` is only valid on create
  operations and `IfExists()` only on drop operations. Using the wrong one
  throws an `InvalidOperationException` during SQL generation.
- `IsClustered` is **SQL Server only**; `AsIdentityColumn`, `IncludeColumns`,
  and `IfNotExists` / `IfExists` work on **both** SQL Server and PostgreSQL.
- The Thinktecture generators derive from the provider's built-in generator
  (`SqlServerMigrationsSqlGenerator` / `NpgsqlMigrationsSqlGenerator`), so all
  standard migration behavior is preserved.
