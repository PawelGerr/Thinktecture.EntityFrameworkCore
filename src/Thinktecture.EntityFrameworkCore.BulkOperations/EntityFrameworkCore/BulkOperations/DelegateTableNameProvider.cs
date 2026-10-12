using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides the table names of <see cref="IBulkOperationTableNameProvider.Create"/> by calling a delegate.
/// </summary>
/// <param name="getTableName">Returns the table name of an entity type.</param>
internal sealed class DelegateTableNameProvider(Func<IEntityType, BulkOperationTableName> getTableName) : IBulkOperationTableNameProvider
{
   public BulkOperationTableName GetTableName(IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      return getTableName(entityType);
   }
}
