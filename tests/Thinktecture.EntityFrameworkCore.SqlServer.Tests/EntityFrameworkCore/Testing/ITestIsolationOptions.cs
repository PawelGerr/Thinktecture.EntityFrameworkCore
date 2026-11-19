using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Testing;

/// <summary>
/// Options for test isolation behavior.
/// </summary>
public interface ITestIsolationOptions
{
   /// <summary>
   /// Deletes all records from the tables using "DELETE".
   /// The tables of temp tables and collection parameters and the tables that <paramref name="filter"/> rejects are skipped.
   /// </summary>
   public static ITestIsolationOptions DeleteData(Predicate<IEntityType> filter)
   {
      return new DeleteAllData(entityType => filter(entityType) && SkipTempTablesAndCollectionParameters(entityType));
   }

   private static bool SkipTempTablesAndCollectionParameters(IEntityType entityType)
   {
      return !entityType.ClrType.IsGenericType
             || (entityType.ClrType.GetGenericTypeDefinition() != typeof(TempTable<>)
                 && entityType.ClrType.GetGenericTypeDefinition() != typeof(TempTable<,>)
                 && entityType.ClrType.GetGenericTypeDefinition() != typeof(ScalarCollectionParameter<>));
   }

   /// <summary>
   /// Cleanup of the database.
   /// </summary>
   ValueTask CleanupAsync(DbContext dbContext, CancellationToken cancellationToken);

   private class DeleteAllData : ITestIsolationOptions
   {
      private static readonly MethodInfo _deleteData = typeof(DeleteAllData).GetMethod(nameof(DeleteDataAsync), BindingFlags.Static | BindingFlags.NonPublic)
                                                       ?? throw new Exception($"Method '{nameof(DeleteDataAsync)}' not found.");

      private readonly Predicate<IEntityType> _filter;
      private readonly ConcurrentDictionary<IEntityType, Func<DbContext, string, CancellationToken, Task>> _deleteDelegatesLookup;

      private IReadOnlyList<IEntityType>? _orderedEntities;

      public DeleteAllData(Predicate<IEntityType> filter)
      {
         _filter = filter;
         _deleteDelegatesLookup = new ConcurrentDictionary<IEntityType, Func<DbContext, string, CancellationToken, Task>>();
      }

      [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
      public async ValueTask CleanupAsync(DbContext dbContext, CancellationToken cancellationToken)
      {
         _orderedEntities ??= dbContext.Model.GetEntityTypesInHierarchicalOrder().Reverse().ToList();

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
}
