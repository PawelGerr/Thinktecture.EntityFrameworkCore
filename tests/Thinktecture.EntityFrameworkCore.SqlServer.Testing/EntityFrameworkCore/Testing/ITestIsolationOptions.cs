using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Options for test isolation behavior.
/// </summary>
public interface ITestIsolationOptions
{
   /// <summary>
   /// No test isolation, i.e. no ambient transaction, no unique schema, no cleanup.
   /// </summary>
   public static readonly ITestIsolationOptions None = new NoIsolation();

   /// <summary>
   /// Test isolation via ambient transaction.
   /// No unique schema, no cleanup.
   /// </summary>
   public static readonly ITestIsolationOptions SharedTablesAmbientTransaction = new ShareTablesIsolation();

   /// <summary>
   /// Rollbacks migrations and then deletes database objects (like tables) with a schema used by the tests.
   /// No ambient transaction; uses unique schema.
   /// </summary>
   public static readonly ITestIsolationOptions RollbackMigrationsAndCleanup = new RollbackMigrationsAndCleanupDatabase();

   /// <summary>
   /// Deletes database objects (like tables) with a schema used by the tests.
   /// No ambient transaction; uses unique schema.
   /// </summary>
   public static readonly ITestIsolationOptions CleanupOnly = new CleanupDatabase();

   /// <summary>
   /// Deletes all records from the tables using "TRUNCATE".
   /// No ambient transaction; no unique schema.
   /// </summary>
   public static readonly ITestIsolationOptions TruncateTables = new TruncateAllTables();

   /// <summary>
   /// Deletes all records from the tables using "DELETE".
   /// No ambient transaction; no unique schema.
   /// </summary>
   public static ITestIsolationOptions DeleteData(
      Predicate<IEntityType>? filter = null,
      Func<IReadOnlyList<IEntityType>, IReadOnlyList<IEntityType>>? modifyDeletionOrder = null)
   {
      return new DeleteAllData(filter is null
                                  ? SkipTempTablesAndCollectionParameters
                                  : entityType => filter(entityType) && SkipTempTablesAndCollectionParameters(entityType),
                               modifyDeletionOrder);
   }

   private static bool SkipTempTablesAndCollectionParameters(IEntityType entityType)
   {
      return !entityType.ClrType.IsGenericType
             || (entityType.ClrType.GetGenericTypeDefinition() != typeof(TempTable<>)
                 && entityType.ClrType.GetGenericTypeDefinition() != typeof(TempTable<,>)
                 && entityType.ClrType.GetGenericTypeDefinition() != typeof(ScalarCollectionParameter<>));
   }

   /// <summary>
   /// Performs custom cleanup.
   /// </summary>
   /// <param name="needsUniqueSchema">Indicator whether the tables require an unique database schema.</param>
   /// <param name="cleanup">Callback that performs the actual cleanup.</param>
   /// <typeparam name="T">Type of the <see cref="DbContext"/></typeparam>
   /// <returns></returns>
   public static ITestIsolationOptions Custom<T>(bool needsUniqueSchema, Func<T, string?, CancellationToken, Task> cleanup)
      where T : DbContext
   {
      return new CustomCleanup<T>(needsUniqueSchema, cleanup);
   }

   /// <summary>
   /// Indicator, whether the database needs cleanup.
   /// </summary>
   bool NeedsAmbientTransaction { get; }

   /// <summary>
   /// Indicator, whether the database needs cleanup.
   /// </summary>
   bool NeedsUniqueSchema { get; }

   /// <summary>
   /// Indicator, whether the database needs cleanup.
   /// </summary>
   bool NeedsCleanup { get; }

   /// <summary>
   /// Cleanup of the database.
   /// </summary>
   ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken);

   private class NoIsolation : ITestIsolationOptions
   {
      public bool NeedsAmbientTransaction => false;
      public bool NeedsUniqueSchema => false;
      public bool NeedsCleanup => false;

      public ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         return ValueTask.CompletedTask;
      }
   }

   private class ShareTablesIsolation : ITestIsolationOptions
   {
      public bool NeedsAmbientTransaction => true;
      public bool NeedsUniqueSchema => false;
      public bool NeedsCleanup => false;

      public ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         return ValueTask.CompletedTask;
      }
   }

   private class RollbackMigrationsAndCleanupDatabase : ITestIsolationOptions
   {
      public bool NeedsAmbientTransaction => false;
      public bool NeedsUniqueSchema => true;
      public bool NeedsCleanup => true;

      public async ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         await RollbackMigrationsAsync(dbContext, cancellationToken);
         await DeleteDatabaseObjectsAsync(dbContext, schema, cancellationToken);
      }
   }

   private class CleanupDatabase : ITestIsolationOptions
   {
      public bool NeedsAmbientTransaction => false;
      public bool NeedsUniqueSchema => true;
      public bool NeedsCleanup => true;

      public async ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         await DeleteDatabaseObjectsAsync(dbContext, schema, cancellationToken);
      }
   }

   private class CustomCleanup<T> : ITestIsolationOptions
      where T : DbContext
   {
      private readonly Func<T, string?, CancellationToken, Task> _cleanup;

      public bool NeedsAmbientTransaction => false;
      public bool NeedsUniqueSchema { get; }
      public bool NeedsCleanup => true;

      public CustomCleanup(bool needsUniqueSchema, Func<T, string?, CancellationToken, Task> cleanup)
      {
         NeedsUniqueSchema = needsUniqueSchema;
         _cleanup = cleanup;
      }

      public async ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         await _cleanup((T)dbContext, schema, cancellationToken);
      }
   }

   private class TruncateAllTables : ITestIsolationOptions
   {
      public bool NeedsAmbientTransaction => false;
      public bool NeedsUniqueSchema => false;
      public bool NeedsCleanup => true;

      [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
      public async ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         foreach (var entityType in dbContext.Model.GetEntityTypesInHierarchicalOrder().Reverse())
         {
            if (entityType.GetTableName() is not null)
               await dbContext.TruncateTableAsync(entityType.ClrType, cancellationToken);
         }
      }
   }

   private class DeleteAllData : ITestIsolationOptions
   {
      private static readonly MethodInfo _deleteData = typeof(DeleteAllData).GetMethod(nameof(DeleteDataAsync), BindingFlags.Static | BindingFlags.NonPublic)
                                                       ?? throw new Exception($"Method '{nameof(DeleteDataAsync)}' not found.");

      private readonly Predicate<IEntityType> _filter;
      private readonly Func<IReadOnlyList<IEntityType>, IReadOnlyList<IEntityType>>? _modifyDeletionOrder;
      private readonly ConcurrentDictionary<IEntityType, Func<DbContext, string, CancellationToken, Task>> _deleteDelegatesLookup;

      private IReadOnlyList<IEntityType>? _orderedEntities;

      public bool NeedsAmbientTransaction => false;
      public bool NeedsUniqueSchema => false;
      public bool NeedsCleanup => true;

      public DeleteAllData(
         Predicate<IEntityType> filter,
         Func<IReadOnlyList<IEntityType>, IReadOnlyList<IEntityType>>? modifyDeletionOrder)
      {
         _filter = filter;
         _modifyDeletionOrder = modifyDeletionOrder;
         _deleteDelegatesLookup = new ConcurrentDictionary<IEntityType, Func<DbContext, string, CancellationToken, Task>>();
      }

      [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
      public async ValueTask CleanupAsync(DbContext dbContext, string? schema, CancellationToken cancellationToken)
      {
         if (_orderedEntities is null)
         {
            var orderedEntities = dbContext.Model.GetEntityTypesInHierarchicalOrder().Reverse().ToList();

            _orderedEntities = _modifyDeletionOrder?.Invoke(orderedEntities) ?? orderedEntities;
         }

         foreach (var entityType in _orderedEntities)
         {
            if (entityType.GetTableName() is null || !_filter(entityType))
               continue;

            var delete = _deleteDelegatesLookup.GetOrAdd(entityType, CreateDelegate);

            await delete(dbContext, entityType.Name, cancellationToken);
         }
      }

      private static Func<DbContext, string, CancellationToken, Task> CreateDelegate(IEntityType type)
      {
         var ctxParam = Expression.Parameter(typeof(DbContext));
         var nameParam = Expression.Parameter(typeof(string));
         var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken));
         var method = _deleteData.MakeGenericMethod(type.ClrType);

         var call = Expression.Call(method, ctxParam, nameParam, cancellationTokenParam);

         return Expression.Lambda<Func<DbContext, string, CancellationToken, Task>>(call, ctxParam, nameParam, cancellationTokenParam).Compile();
      }

      private static async Task DeleteDataAsync<T>(DbContext dbContext, string name, CancellationToken cancellationToken)
         where T : class
      {
         await dbContext.Set<T>(name).ExecuteDeleteAsync(cancellationToken);
      }
   }

   /// <summary>
   /// Rollbacks all migrations.
   /// </summary>
   protected static async Task RollbackMigrationsAsync(DbContext dbContext, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(dbContext);

      await dbContext.GetService<IMigrator>().MigrateAsync("0", cancellationToken);
   }

   /// <summary>
   /// Deletes database objects (like tables) with provided <paramref name="schema"/>.
   /// </summary>
   protected static async Task DeleteDatabaseObjectsAsync(DbContext ctx, string? schema, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      if (schema is not null)
      {
         var sqlHelper = ctx.GetService<ISqlGenerationHelper>();

         await ctx.Database.ExecuteSqlRawAsync(GetSqlForCleanup(), new object[] { new SqlParameter("@schema", schema) }, cancellationToken);
         await ctx.Database.ExecuteSqlRawAsync(GetDropSchemaSql(sqlHelper, schema), cancellationToken);
      }
   }

   private static string GetSqlForCleanup()
   {
      return """
             DECLARE @crlf NVARCHAR(MAX) = CHAR(13) + CHAR(10);
             DECLARE @sql NVARCHAR(MAX);
             DECLARE @cursor CURSOR

             -- Drop Constraints
             SET @cursor = CURSOR FAST_FORWARD FOR
             SELECT DISTINCT sql = 'ALTER TABLE ' + QUOTENAME(tc.TABLE_SCHEMA) + '.' +  QUOTENAME(tc.TABLE_NAME) + ' DROP ' + QUOTENAME(rc.CONSTRAINT_NAME) + ';' + @crlf
             FROM
             	INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
             	LEFT JOIN
             		INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
             		ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
             WHERE
                tc.TABLE_SCHEMA = @schema

             OPEN @cursor FETCH NEXT FROM @cursor INTO @sql

             WHILE (@@FETCH_STATUS = 0)
             BEGIN
             Exec sp_executesql @sql
             FETCH NEXT FROM @cursor INTO @sql
             END

             CLOSE @cursor
             DEALLOCATE @cursor

             -- Drop Views
             SELECT	@sql = N'';
             SELECT @sql = @sql + 'DROP VIEW ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) +';' + @crlf
             FROM
             	SYS.VIEWS
             WHERE
                schema_id = SCHEMA_ID(@schema)

             EXEC(@sql);

             -- Drop Functions
             SELECT @sql = N'';
             SELECT @sql = @sql + N' DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
             FROM sys.objects
             WHERE type_desc LIKE '%FUNCTION%'
                AND schema_id = SCHEMA_ID(@schema);

             EXEC(@sql);

             -- Disable temporal tables
             SELECT	@sql = N'';
             SELECT @sql = @sql + 'IF OBJECTPROPERTY(OBJECT_ID(''' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) +'''), ''TableTemporalType'') = 2' + @crlf
                         + ' ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) +' SET (SYSTEM_VERSIONING = OFF);' + @crlf
             FROM
             	SYS.TABLES
             WHERE
                schema_id = SCHEMA_ID(@schema)

             EXEC(@sql);

             -- Drop tables
             SELECT	@sql = N'';
             SELECT @sql = @sql + 'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) +';' + @crlf
             FROM
             	SYS.TABLES
             WHERE
                schema_id = SCHEMA_ID(@schema)

             EXEC(@sql);
             """;
   }

   private static string GetDropSchemaSql(ISqlGenerationHelper sqlHelper, string schema)
   {
      ArgumentNullException.ThrowIfNull(sqlHelper);

      return $"""
              IF SCHEMA_ID('{sqlHelper.DelimitIdentifier(schema)}') IS NOT NULL
                 DROP SCHEMA {sqlHelper.DelimitIdentifier(schema)};
              """;
   }
}
