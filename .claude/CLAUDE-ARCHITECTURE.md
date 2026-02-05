# CLAUDE-ARCHITECTURE.md

Architecture and EF Core integration patterns for Thinktecture.EntityFrameworkCore.

## Package Dependency Graph

```
Relational                          Foundation layer (base abstractions)
  │
  └─ BulkOperations                 Provider-agnostic bulk operation interfaces
       │
       ├─ SqlServer                 SQL Server implementation (SqlBulkCopy, MERGE)
       │    └─ SqlServer.Testing    SQL Server test utilities
       │
       └─ Sqlite.Core              SQLite foundation
            └─ Sqlite              Full SQLite package
                 └─ Sqlite.Testing SQLite test utilities

Testing                             Shared test infrastructure (used by *.Testing)
```

Each layer adds provider-specific implementations to the abstractions defined above it. The `Relational` package has no provider dependency; `BulkOperations` depends only on `Relational`.

## Core Feature Areas

### 1. Bulk Operations
- **Interfaces**: `IBulkInsertExecutor`, `IBulkUpdateExecutor`, `IBulkInsertOrUpdateExecutor`, `ITruncateTableExecutor`
- **SQL Server**: Uses `SqlBulkCopy` for inserts, MERGE statements for updates/upserts
- **SQLite**: Uses batched INSERT/UPDATE statements
- **Property selection**: `IEntityPropertiesProvider` with `IncludingEntityPropertiesProvider` / `ExcludingEntityPropertiesProvider`
- **Entry point**: Extension methods on `DbContext` in `BulkOperationsDbContextExtensions`

### 2. Temp Tables
- **Creation**: `ITempTableCreator` creates temp tables; `ITempTableReference` manages cleanup via `IAsyncDisposable`
- **Queryable wrapper**: `ITempTableQuery<T>` wraps `IQueryable<T>` with automatic cleanup on dispose
- **Bulk population**: `ITempTableBulkInsertExecutor` for fast data loading
- **Name management**: `TempTableSuffixLeasing` + `TempTableSuffixCache` prevent name conflicts in concurrent scenarios
- **Entry point**: `dbContext.BulkInsertIntoTempTableAsync(entities)` and `dbContext.BulkInsertValuesIntoTempTableAsync(values)`

### 3. Window Functions
- **Fluent API**: `EF.Functions.RowNumber()`, `EF.Functions.Average()`, etc. with `PartitionBy()` and `OrderBy()`
- **Translation**: `RelationalDbFunctionsTranslator` translates to `WindowFunctionExpression` / `WindowFunctionPartitionByExpression`
- **Both providers**: Implemented for SQL Server and SQLite via query translators

### 4. LEFT JOIN
- **Extension methods**: `source.LeftJoin(inner, outerKey, innerKey, resultSelector)` on `IQueryable<T>`
- **Result type**: `LeftJoinResult<TOuter, TInner>` with nullable inner entity
- **Translation**: Custom expression visitors convert to SQL LEFT JOIN

### 5. Table Hints (SQL Server only)
- **API**: `query.WithTableHints(SqlServerTableHint.NoLock)`
- **Enum values**: `NoLock`, `ReadPast`, `UpdLock`, `HoldLock`, `RowLock`, `PageLock`, `TabLock`, etc.

### 6. Nested Transactions
- **Manager**: `NestedRelationalTransactionManager` replaces EF Core's default transaction manager
- **Root transactions**: `RootNestedDbContextTransaction` wraps actual DB transaction
- **Child transactions**: `ChildNestedDbContextTransaction` are logical (no actual nested SQL transactions)

### 7. Collection Parameters
- **Scalar**: `ScalarCollectionParameter<T>` for passing value lists to queries
- **JSON (SQL Server)**: `JsonCollectionParameter` serializes collections as JSON
- **Factory**: `ICollectionParameterFactory` creates provider-specific parameters

### 8. Tenant Database Support
- **Interface**: `ITenantDatabaseProvider` provides per-tenant database names
- **Query integration**: Injects tenant info into query context to prevent cache collisions

## EF Core Extension Architecture

### DbContextOptionsExtension Pattern

This is the primary integration point with EF Core. The library uses a **three-tier extension hierarchy**:

```
DbContextOptionsExtensionBase (shared utilities)
  ├─ RelationalDbContextOptionsExtension (provider-agnostic features)
  ├─ SqlServerDbContextOptionsExtension (SQL Server features)
  └─ SqliteDbContextOptionsExtension (SQLite features)
```

Each extension:
1. Implements `IDbContextOptionsExtension`
2. Has **boolean feature flags** (e.g., `AddWindowFunctionsSupport`, `AddBulkOperationSupport`)
3. Registers services in `ApplyServices(IServiceCollection services)`
4. Has an inner `DbContextOptionsExtensionInfo` class for service provider caching

**Feature flag cascading** - dependent features auto-enable prerequisites:
```csharp
public bool AddCustomQueryableMethodTranslatingExpressionVisitorFactory
{
    get => _addCustomQueryableMethodTranslatingExpressionVisitorFactory
           || AddBulkOperationSupport
           || AddWindowFunctionsSupport
           || AddTableHintSupport;
}
```

### User-Facing Registration API

Users enable features via `DbContextOptionsBuilder` extension methods:
```csharp
services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(connectionString)
           .AddBulkOperationSupport()
           .AddWindowFunctionsSupport()
           .AddTableHintSupport()
           .AddNestedTransactionsSupport()
           .AddSchemaRespectingComponents()
           .AddTenantDatabaseSupport<MyTenantProvider>());
```

These extension methods add or update the appropriate `DbContextOptionsExtension` on the options builder.

### Service Registration in ApplyServices

When EF Core builds the service provider, it calls `ApplyServices()` on each extension. Services are registered conditionally based on feature flags:

```csharp
public override void ApplyServices(IServiceCollection services)
{
    if (AddBulkOperationSupport)
    {
        services.Add<IConventionSetPlugin, BulkOperationConventionSetPlugin>(GetLifetime<IConventionSetPlugin>());
        services.TryAddScoped<ITempTableCreator, SqlServerTempTableCreator>();
        services.TryAddScoped<SqlServerBulkOperationExecutor>();
        // Register executor as multiple interfaces (single implementation)
        services.TryAddScoped<IBulkInsertExecutor>(p => p.GetRequiredService<SqlServerBulkOperationExecutor>());
        services.TryAddScoped<IBulkUpdateExecutor>(p => p.GetRequiredService<SqlServerBulkOperationExecutor>());
        // ...
    }

    if (AddWindowFunctionsSupport)
        services.Add<IMethodCallTranslatorPlugin, RelationalMethodCallTranslatorPlugin>(...);
}
```

### Component Decorator Pattern

The library uses a **decorator pattern** to non-destructively wrap EF Core's internal services. This is the key mechanism in `RelationalDbContextComponentDecorator`:

```csharp
// What it does:
// 1. Finds EF Core's existing registration for TService
// 2. Re-registers the original implementation under its own type
// 3. Replaces the TService registration with a decorator that wraps the original

ComponentDecorator.RegisterDecorator<IModelCustomizer>(
    services, typeof(DefaultSchemaModelCustomizer<>));

// Result: EF Core resolves IModelCustomizer → DefaultSchemaModelCustomizer<OriginalCustomizer>
//         DefaultSchemaModelCustomizer<T> receives the original customizer via DI
```

**Used for:**
- `IModelCustomizer` → `DefaultSchemaModelCustomizer<T>` (adds default schema)
- `IModelCacheKeyFactory` → `DefaultSchemaRespectingModelCacheKeyFactory<T>` (includes schema in cache key)
- `IMigrationsAssembly` → `DefaultSchemaRespectingMigrationAssembly<T>` (applies schema to migrations)
- `IQueryContextFactory` → `ThinktectureRelationalQueryContextFactory<T>` (adds tenant params)

### Service Replacement with Validation

For direct service replacement (not decoration), the library validates that the existing registration is what's expected:

```csharp
protected void AddWithCheck<TService, TImplementation, TExpectedImplementation>(IServiceCollection services)
{
    // Verifies the current registration is TExpectedImplementation before replacing with TImplementation
    // Throws InvalidOperationException if unexpected service is registered
}
```

### Service Lifetime Discovery

Custom services must match EF Core's expected lifetime for each service type:

```csharp
protected ServiceLifetime GetLifetime<TService>()
{
    // Looks up TService in EF Core's RelationalServices/CoreServices registries
    // Returns Singleton, Scoped, or Transient as defined by EF Core
}
```

### Singleton Options Bridge

Configuration from `IDbContextOptionsExtension` (scoped) is bridged to singleton services via `ISingletonOptions`:

```csharp
public class RelationalDbContextOptionsExtensionOptions : ISingletonOptions
{
    public bool WindowFunctionsSupportEnabled { get; private set; }

    public void Initialize(IDbContextOptions options)
    {
        var extension = options.FindExtension<RelationalDbContextOptionsExtension>();
        WindowFunctionsSupportEnabled = extension.AddWindowFunctionsSupport;
    }
}
```

## Query Translation Pipeline

```
LINQ Expression
  └─ IQueryableMethodTranslatingExpressionVisitorFactory
       └─ ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitor
            └─ Handles custom methods (AsSubQuery, LeftJoin)

Method Calls (EF.Functions.*)
  └─ IMethodCallTranslatorPlugin
       └─ RelationalMethodCallTranslatorPlugin
            └─ RelationalDbFunctionsTranslator
                 └─ Produces: WindowFunctionExpression, RowNumberExpression, etc.

SQL Generation
  └─ IQuerySqlGeneratorFactory
       └─ ThinktectureSqlServerQuerySqlGeneratorFactory
            └─ Custom QuerySqlGenerator
                 └─ Handles window functions, table hints, tenant databases
```

## Convention Set Plugin

Model building conventions are extended via `IConventionSetPlugin`:

```csharp
public class BulkOperationConventionSetPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        if (_options.ConfigureTempTablesForPrimitiveTypes)
            conventionSet.ModelInitializedConventions.Add(TempTableConvention.Instance);
        return conventionSet;
    }
}
```

## Migration Customization

- `ThinktectureSqlServerMigrationsSqlGenerator` extends `SqlServerMigrationsSqlGenerator`
- Overrides `Generate(CreateTableOperation)`, `Generate(DropTableOperation)` for conditional SQL (IF EXISTS/IF NOT EXISTS)
- `DefaultSchemaRespectingMigrationAssembly<T>` injects `IDbDefaultSchema` into migration instances at runtime

## How to Add a New Feature

1. **Add boolean flag** to the appropriate `DbContextOptionsExtension` class
2. **Add user-facing extension method** on `DbContextOptionsBuilder` (in `Extensions/` directory)
3. **Implement ApplyServices() logic** - register services conditionally based on flag
4. **Update ExtensionInfo** - add flag to `GetServiceProviderHashCode()`, `ShouldUseSameServiceProvider()`, and `PopulateDebugInfo()`
5. **Implement the feature** using EF Core's extension points:
   - Query translation → `IMethodCallTranslatorPlugin` or `IQueryableMethodTranslatingExpressionVisitorFactory`
   - Model building → `IConventionSetPlugin`
   - SQL generation → `IQuerySqlGeneratorFactory`
   - Service wrapping → Component Decorator pattern
6. **Add tests** in the appropriate test project with SQL verification
7. **Use `GetLifetime<T>()`** to match EF Core's expected service lifetime

## Entity Data Reader (ADO.NET Bridge)

`IEntityDataReader` / `EntityDataReader` expose entity collections as `IDataReader` for use with `SqlBulkCopy`:
- `IEntityDataReaderFactory` creates readers from entity collections
- `PropertyWithNavigations` handles complex property paths including navigations
- Supports regular properties, shadow properties, and owned entity navigation properties
