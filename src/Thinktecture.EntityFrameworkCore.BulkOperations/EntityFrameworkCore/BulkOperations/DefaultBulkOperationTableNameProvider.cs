using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Resolves the table name and the schema from the EF Core model.
/// </summary>
internal sealed class DefaultBulkOperationTableNameProvider : IBulkOperationTableNameProvider
{
   public BulkOperationTableName GetTableName(IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      if (entityType.IsTempTableEntityType())
         throw new InvalidOperationException($"The entity type '{entityType.ClrType.ShortDisplayName()}' is configured as a temp table entity and does not have a regular table mapping. Provide the target table name via 'TableNameProvider' in the options, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'.");

      var tableName = entityType.GetTableName() ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");

      return new BulkOperationTableName(tableName, entityType.GetSchema());
   }
}
