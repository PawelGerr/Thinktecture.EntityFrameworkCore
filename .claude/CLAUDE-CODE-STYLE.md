# CLAUDE-CODE-STYLE.md

Code conventions for the Thinktecture.EntityFrameworkCore repository. These are derived from actual patterns in the codebase.

## Language & Framework

- **C# 14.0** (`LangVersion 14.0` in Directory.Build.props)
- **Target framework**: `net10.0`
- **Nullable reference types**: Enabled globally
- **Implicit usings**: Enabled (auto-imports `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading`, `System.Threading.Tasks`, etc.)
- **Global using**: `Microsoft.EntityFrameworkCore` is globally imported in all projects via Directory.Build.props

## Namespace Conventions

**File-scoped namespaces** - always use the `;` syntax:
```csharp
namespace Thinktecture.EntityFrameworkCore.BulkOperations;
```

**Root namespace is `Thinktecture`** (set in Directory.Build.props). Namespaces match the folder structure within each project.

**Public extension methods** are placed in the root `Thinktecture` namespace with a ReSharper suppression:
```csharp
// ReSharper disable once CheckNamespace
namespace Thinktecture;

public static class BulkOperationsDbContextExtensions
{
    // Extension methods here
}
```

This makes extension methods discoverable without users needing extra `using` statements.

**Internal types** use their full feature-specific namespace:
```csharp
namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal sealed class ExcludingEntityPropertiesProvider : IEntityPropertiesProvider
```

## Visibility

- **Default to `internal`** - Types are `internal` unless they need to be consumed by other packages or end users
- **Public**: Interfaces that define the API contract (`IBulkInsertExecutor`), extension method classes, option types, enums, exceptions
- **Internal**: Implementation classes, factories, translators, visitors, context factories
- **Sealed**: Use `sealed` on classes that should not be inherited (most `internal` implementations, helpers)
- **No `InternalsVisibleTo`**: Internal types are not exposed between projects

```csharp
// Public - API contract
public interface IBulkInsertExecutor { ... }

// Internal sealed - implementation
internal sealed class SqlServerBulkOperationContextFactoryForEntities : ISqlServerBulkOperationContextFactory { ... }

// Public sealed - user-facing configuration
public sealed class SqlServerBulkInsertOptions : IBulkInsertOptions { ... }
```

## Null Handling

**Primary pattern**: `ArgumentNullException.ThrowIfNull()` for method parameters:
```csharp
public static async Task BulkInsertAsync<T>(this DbContext ctx, ...)
{
    ArgumentNullException.ThrowIfNull(ctx);
    ArgumentNullException.ThrowIfNull(entities);
    // ...
}
```

**Secondary pattern**: Inline throw expression for constructor assignments:
```csharp
public DbDefaultSchema(string schema)
{
    Schema = schema ?? throw new ArgumentNullException(nameof(schema));
}
```

Both patterns coexist; prefer `ThrowIfNull()` for new code.

## Async Patterns

**Every I/O method** must be async with `CancellationToken`:
```csharp
public async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(
    IEnumerable<T> entities,
    ITempTableBulkInsertOptions options,
    CancellationToken cancellationToken = default)
    where T : class
```

**Rules:**
- Always pass `cancellationToken` through to all awaited calls
- Use `Task` return type (not `void`) for async methods
- Provide `CancellationToken cancellationToken = default` as last parameter
- Avoid sync-over-async

## Extension Method Pattern

Extension method classes follow a consistent structure:

```csharp
// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbContext"/>.
/// </summary>
public static class BulkOperationsDbContextExtensions
{
    /// <summary>
    /// Creates a temp table.
    /// </summary>
    /// <param name="ctx">Database context to use.</param>
    /// <param name="options">Options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Type of entity.</typeparam>
    /// <returns>A reference to the created temp table.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
    public static async Task<ITempTableReference> CreateTempTableAsync<T>(
        this DbContext ctx,
        Action<TempTableCreationOptions>? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(ctx);
        // ...
    }
}
```

**Key aspects:**
- Namespace: `Thinktecture` (root)
- Static class named `{Feature}{Target}Extensions`
- Full XML documentation on every public method
- Null checks on `this` parameter
- Multiple overloads for convenience (with/without options, with/without cancellation token)
- Return `this` or fluent types for chaining where appropriate

## DI Registration Pattern

Registration extension methods follow `AddXxx` naming:

```csharp
public static IServiceCollection AddTempTableSuffixComponents(this IServiceCollection services)
{
    services.AddScoped<TempTableSuffixLeasing>();
    services.AddSingleton<TempTableSuffixCache>();
    return services;
}
```

User-facing feature registration is via `DbContextOptionsBuilder` extensions:
```csharp
public static DbContextOptionsBuilder<TContext> AddBulkOperationSupport<TContext>(
    this DbContextOptionsBuilder<TContext> builder)
    where TContext : DbContext
{
    // Adds or updates the DbContextOptionsExtension
}
```

## XML Documentation

`GenerateDocumentationFile` is enabled globally. All public members must have XML docs.

**Required elements:**
- `<summary>` on every public type and member
- `<param name="...">` for every parameter
- `<typeparam name="...">` for every generic type parameter
- `<returns>` for methods with return values
- `<exception cref="...">` for documented exceptions

**Formatting conventions:**
- Use `<c>null</c>` for null references in docs
- Use `<see cref="TypeName"/>` for cross-references
- Use `<paramref name="paramName"/>` for parameter references
- Use `<typeparamref name="T"/>` for type parameter references

```csharp
/// <summary>
/// Bulk inserts <paramref name="entities"/> into a temp table.
/// </summary>
/// <param name="ctx">Database context to use.</param>
/// <param name="entities">Entities to insert.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <typeparam name="T">Type of the entity.</typeparam>
/// <returns>An <see cref="ITempTableQuery{T}"/> for querying the temp table.</returns>
/// <exception cref="ArgumentNullException">
/// <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.
/// </exception>
```

## Type Choices

**Records** - for simple data-transfer / value-like types:
```csharp
public record TempTableInfo(string Name);
```

**Readonly structs** - for frequently-used value types with custom equality:
```csharp
public readonly struct PropertyWithNavigations : IEquatable<PropertyWithNavigations>
{
    // ...
}
```

**Sealed classes** - for final implementations and helper types:
```csharp
internal sealed class ExcludingEntityPropertiesProvider : IEntityPropertiesProvider { ... }
public sealed class DbDefaultSchema : IDbDefaultSchema { ... }
```

**Static classes** - only for extension method containers.

## Error Handling

**Custom exceptions with `[CallerArgumentExpression]`:**
```csharp
public class EntityTypeNotFoundException : ArgumentException
{
    public EntityTypeNotFoundException(
        IEntityType entityType,
        [CallerArgumentExpression("entityType")] string? paramName = null)
        : base($"Entity type '{entityType.ShortDisplayName()}' not found.", paramName)
    {
    }
}
```

**Standard exceptions used:**
- `ArgumentNullException` - null parameters
- `ArgumentException` - invalid parameter values
- `InvalidOperationException` - invalid state
- `NotSupportedException` - unimplemented/unsupported features

## Options Pattern

Feature-specific options use strongly-typed classes:
```csharp
public sealed class SqlServerBulkInsertOptions : IBulkInsertOptions
{
    public IEntityPropertiesProvider? PropertiesToInsert { get; set; }
    // provider-specific options...
}
```

User configuration via callback:
```csharp
await dbContext.BulkInsertAsync(entities, options =>
{
    options.PropertiesToInsert = IncludingEntityPropertiesProvider.Include(e => e.Name);
});
```

## Singleton Instance Pattern

For stateless implementations:
```csharp
internal sealed class SqlServerBulkOperationContextFactoryForEntities
    : ISqlServerBulkOperationContextFactory
{
    public static readonly ISqlServerBulkOperationContextFactory Instance = new SqlServerBulkOperationContextFactoryForEntities();

    private SqlServerBulkOperationContextFactoryForEntities() { }
}
```

## Private Constructor for Non-Instantiable Types

Helper/marker classes that should not be instantiated:
```csharp
public sealed class WindowFunctionOrderByClause
{
    private WindowFunctionOrderByClause() { }
    // Static methods or used as generic constraint marker
}
```

## Source Generators

The codebase uses `[GeneratedRegex]` with `partial` classes:
```csharp
public partial class WindowFunction
{
    [GeneratedRegex(@"...")]
    private static partial Regex MyRegex();
}
```

## Pragma / ReSharper Directives

Used sparingly and scoped:
```csharp
#pragma warning disable CS1591  // Missing XML comment
// ...code...
#pragma warning restore CS1591

// ReSharper disable once CheckNamespace
// ReSharper disable ArrangeMethodOrOperatorBody
```

## Suppressions Summary

**Global (Directory.Build.props):**
- `CA1303` - Do not pass literals as localized parameters
- `MSB3884` - Duplicated props warning

**Tests only (tests/Directory.Build.props):**
- `CA1062` - Validate arguments of public methods
- `EF1002` - EF design pattern members
- `xUnit1041` - Non-standard test class suffix

**Per test project (.csproj):**
- `CS1591` - Missing XML comments
- `CA2000` - Dispose objects before losing scope

## Package Management

- All NuGet package versions are centralized in `Directory.Packages.props` at the repo root
- `.csproj` files reference packages **without version numbers**:
  ```xml
  <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
  ```
- To update a dependency version, change it only in `Directory.Packages.props`
- Project references use relative paths:
  ```xml
  <ProjectReference Include="..\Thinktecture.EntityFrameworkCore.Relational\Thinktecture.EntityFrameworkCore.Relational.csproj" />
  ```

## Expression-Bodied Members

Used conservatively. Prefer full method bodies for clarity; expression bodies for simple one-liners:
```csharp
// OK for simple properties
public string Name => _name;

// Prefer full body for anything non-trivial
public async Task<ITempTableQuery<T>> BulkInsertIntoTempTableAsync<T>(...)
{
    ArgumentNullException.ThrowIfNull(ctx);
    var creator = ctx.GetService<ITempTableCreator>();
    return await creator.CreateAsync<T>(entities, options, cancellationToken);
}
```
