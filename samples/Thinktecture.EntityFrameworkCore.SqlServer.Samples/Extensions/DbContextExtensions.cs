using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Thinktecture.Database;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

public static class DbContextExtensions
{
   private static readonly MethodInfo _cast = typeof(Enumerable).GetMethods().Single(m => m.Name == nameof(Enumerable.Cast));
   private static readonly MethodInfo _bulkInsert = typeof(BulkOperationsDbContextExtensions).GetMethods().Single(m => m.Name == nameof(BulkOperationsDbContextExtensions.BulkInsertAsync) && m.GetParameters().All(p => p.ParameterType != typeof(IBulkInsertOptions)));
   private static readonly ConcurrentDictionary<Type, Func<DbContext, IEnumerable, CancellationToken, Task>> _bulkInsertDelegates = new();

   public static async Task BulkInsertHierarchyAsync<T>(this DemoDbContext ctx, IEnumerable<T> entities, CancellationToken cancellationToken)
      where T : notnull
   {
      foreach (var group in entities.GroupBy(e => e.GetType()))
      {
         var bulkInsertDelegate = _bulkInsertDelegates.GetOrAdd(group.Key, CreateBulkInsertDelegate);

         await bulkInsertDelegate(ctx, group, cancellationToken);
      }
   }

   private static Func<DbContext, IEnumerable, CancellationToken, Task> CreateBulkInsertDelegate(Type entityType)
   {
      var bulkInsertMethod = _bulkInsert.MakeGenericMethod(entityType);
      var castMethod = _cast.MakeGenericMethod(entityType);

      var ctxParam = Expression.Parameter(typeof(DbContext));
      var entitiesParam = Expression.Parameter(typeof(IEnumerable));
      var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken));

      var propertiesToInsertConstant = Expression.Constant(null, bulkInsertMethod.GetParameters()[2].ParameterType);

      var castedEntities = Expression.Call(null, castMethod, entitiesParam);
      var bulkInsertCall = Expression.Call(null, bulkInsertMethod, ctxParam, castedEntities, propertiesToInsertConstant, cancellationTokenParam);

      return Expression.Lambda<Func<DbContext, IEnumerable, CancellationToken, Task>>(bulkInsertCall, ctxParam, entitiesParam, cancellationTokenParam)
                       .Compile();
   }
}
