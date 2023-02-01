using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture;

public class DeleteTestData : ITestIsolationOptions
{
   private static readonly MethodInfo _deleteData = typeof(DeleteTestData).GetMethod(nameof(DeleteDataAsync), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("Method 'DeleteDataAsync' not found.");

   public bool NeedsAmbientTransaction => false;
   public bool NeedsUniqueSchema => false;
   public bool NeedsCleanup => true;

   public async ValueTask CleanupAsync(DbContext dbContext, string schema, CancellationToken cancellationToken)
   {
      foreach (var entityType in dbContext.Model.GetEntityTypesInHierarchicalOrder().Reverse())
      {
         if (!String.IsNullOrWhiteSpace(entityType.GetTableName())
             && entityType.ClrType != typeof(TestEntityWithCollation)
             && entityType.ClrType != typeof(CustomTempTable)
             && entityType.ClrType != typeof(OwnedEntity)
             && entityType.ClrType != typeof(OwnedEntity_Owns_Inline)
             && entityType.ClrType != typeof(OwnedEntity_Owns_SeparateOne)
             && entityType.ClrType != typeof(OwnedEntity_Owns_SeparateMany)
             && entityType.ClrType != typeof(MyParameter)
             && entityType.ClrType != typeof(TestTemporalTableEntity)
             && (!entityType.ClrType.IsGenericType ||
                 (entityType.ClrType.GetGenericTypeDefinition() != typeof(TempTable<>)
                  && entityType.ClrType.GetGenericTypeDefinition() != typeof(TempTable<,>)
                  && entityType.ClrType.GetGenericTypeDefinition() != typeof(ScalarCollectionParameter<>))))
         {
            var task = (Task?)_deleteData.MakeGenericMethod(entityType.ClrType).Invoke(null, new object?[] { dbContext, cancellationToken })
                       ?? throw new Exception("Task cannot be null");

            await task;
         }
      }
   }

   private static async Task DeleteDataAsync<T>(DbContext dbContext, CancellationToken cancellationToken)
      where T : class
   {
      await dbContext.Set<T>().ExecuteDeleteAsync(cancellationToken);
   }
}
