# Nested (virtual) Transactions

Call `BeginTransaction` more than once on the same `DbContext`. The **first**
call starts a real database transaction (the *root*); every **further** call
creates a *virtual child transaction*. The underlying DB transaction commits
**only if** the root and all children commit — otherwise it rolls back. On
**all providers** (SQL Server, PostgreSQL, SQLite) child transactions are
purely logical (tracked in-memory) — they are **not** mapped to database
savepoints.

> Use this when code paths each want to "own" a transaction (e.g. a service
> method that may be called standalone or already inside an outer transaction)
> without having to thread a transaction object through every call.

**Enable:** `AddNestedTransactionSupport()` on the **outer**
`DbContextOptionsBuilder` (provider-agnostic — *not* inside
`UseSqlServer`/`UseNpgsql`/`UseSqlite`).
**Namespace:** `using Thinktecture;`

## API

Enabling is the only Thinktecture-specific call; once enabled you use the
**standard EF Core transaction API** unchanged. The extension method (both
generic and non-generic forms) is:

```csharp
DbContextOptionsBuilder<T> AddNestedTransactionSupport<T>(
   this DbContextOptionsBuilder<T> builder,
   bool addNestedTransactionsSupport = true) where T : DbContext;

DbContextOptionsBuilder AddNestedTransactionSupport(
   this DbContextOptionsBuilder builder,
   bool addNestedTransactionsSupport = true);
```

Once enabled, the usual EF Core members work and become nestable:

```csharp
ctx.Database.BeginTransaction();                 // + async, + IsolationLevel overloads
await ctx.Database.BeginTransactionAsync(cancellationToken);
tx.Commit();   // or await tx.CommitAsync();
tx.Rollback(); // or await tx.RollbackAsync();
tx.Dispose();  // or await tx.DisposeAsync();
```

Internally these are served by `NestedRelationalTransactionManager` (registered
as EF Core's `IDbContextTransactionManager`), so nothing in your call sites
changes — only the registration.

## Examples

### Enable the feature

```csharp
using Thinktecture;

services.AddDbContext<MyDbContext>(builder => builder
   .UseSqlServer(connectionString)        // or .UseNpgsql(...) / .UseSqlite(...)
   .AddNestedTransactionSupport());        // outer builder, provider-agnostic
```

### Nesting two transactions

```csharp
using Thinktecture;

// starts a real database transaction (root)
await using var rootTx = await ctx.Database.BeginTransactionAsync();

ctx.Orders.Add(new Order { /* ... */ });
await ctx.SaveChangesAsync();

// creates a virtual child transaction (no new DB transaction)
await using var childTx = await ctx.Database.BeginTransactionAsync();

ctx.OrderLines.Add(new OrderLine { /* ... */ });
await ctx.SaveChangesAsync();

await childTx.CommitAsync();   // logical commit only
await rootTx.CommitAsync();    // commits the real DB transaction here
```

### Commit / rollback semantics

```csharp
// case 1 — all commit  → DB transaction COMMITS
await childTx.CommitAsync();
await rootTx.CommitAsync();

// case 2 — outer rolls back → DB transaction ROLLS BACK
await childTx.CommitAsync();
await rootTx.RollbackAsync();

// case 3 — both roll back → DB transaction ROLLS BACK
await childTx.RollbackAsync();
await rootTx.RollbackAsync();

// case 4 — child rolled back, root tries to commit → THROWS
await childTx.RollbackAsync();
await rootTx.CommitAsync(); // throws TransactionAbortedException;
                            // the DB transaction is rolled back (e.g. on Dispose)
```

### Isolation level is validated against the root

```csharp
await using var rootTx = await ctx.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

// a child may not request a STRONGER level than its root:
await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable);
// throws InvalidOperationException:
//   "The isolation level 'RepeatableRead' of the parent transaction is not
//    compatible to the provided isolation level 'Serializable'."
```

## Notes & pitfalls

- **Enable on the outer builder.** `AddNestedTransactionSupport()` is
  provider-agnostic; call it on the `DbContextOptionsBuilder`, not on the
  `UseXxx(...)` sub-builder. Without it, a second `BeginTransaction` throws the
  usual EF Core "transaction already started" error.
- **A rolled-back (or disposed-without-commit) child poisons the whole tree.**
  If any child rolls back, committing the root throws
  `TransactionAbortedException` and the real DB transaction is rolled back —
  even if a *sibling* child committed. Disposing a child without committing
  counts as a rollback.
- **Dispose semantics.** A transaction that is disposed without an explicit
  `Commit` is treated as rolled back. Disposing the root disposes all
  outstanding children and rolls back the DB transaction.
- **Children are logical on every provider.** A child rollback aborts the entire
  transaction tree — the DB transaction can only be committed if everyone
  commits. There is **no** automatic per-child savepoint on any provider
  (including PostgreSQL). If you need true partial rollback, use EF Core's
  standard savepoint API explicitly (`tx.CreateSavepoint("name")` /
  `tx.RollbackToSavepoint("name")`).
- **`DbContext` is not thread-safe** — never share the context or its
  transactions across threads.
- Same isolation-level rule for child transactions: a child may not request a
  level incompatible with (stronger than) the root.
