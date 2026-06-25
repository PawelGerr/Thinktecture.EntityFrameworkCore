# Aggregate Functions (GROUP BY)

PostgreSQL-only GROUP BY aggregate helpers exposed via `EF.Functions`. Require `using Thinktecture;`.
**Always available** on the PostgreSQL provider — no setup/feature flag.

## Bitwise aggregates — `bit_or` / `bit_and` / `bit_xor`

| Method | SQL | Returns |
|--------|-----|---------|
| `EF.Functions.BitOr(g.Select(e => e.Col))`  | `bit_or(col)`  | bitwise OR of the group |
| `EF.Functions.BitAnd(g.Select(e => e.Col))` | `bit_and(col)` | bitwise AND of the group |
| `EF.Functions.BitXor(g.Select(e => e.Col))` | `bit_xor(col)` | bitwise XOR of the group (PostgreSQL 14+) |

Generic `BitOr<T>(IEnumerable<T>)` returning `T`. Supported `T`: `short`/`int`/`long`, their
nullable forms, and `[Flags]` enums backed by an integer type (the enum round-trips).

```csharp
var flagsPerOwner = await ctx.Objects
   .GroupBy(o => o.OwnerId)
   .Select(g => new
   {
      OwnerId = g.Key,
      Flags   = EF.Functions.BitOr(g.Select(e => e.Flags))   // bit_or(flags)
   })
   .ToListAsync();
```

```sql
SELECT o.owner_id, bit_or(o.flags)
FROM objects AS o
GROUP BY o.owner_id
```

### Rules / gotchas

- **GROUP BY form only** — there is no window (`OVER (...)`) form for these functions.
- Always call inside `GroupBy(...).Select(...)` passing `g.Select(e => e.Col)` (or `g` directly).
- `NULL`s are ignored; the result is `NULL` only when every value in the group is `NULL`.
- No cast is emitted; the input type is preserved and mapped back to your CLR type.
- PostgreSQL only. Calling these on SQL Server / SQLite will not translate.
