using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides a table name for the root entity type only and throws for owned entity types that are mapped to separate tables.
/// </summary>
internal abstract class RootEntityTableNameProviderBase : IBulkOperationTableNameProvider
{
   public BulkOperationTableName GetTableName(IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      var ownership = entityType.FindOwnership();

      if (ownership is not null)
         throw new InvalidOperationException($"The options 'TableName', 'Schema' and 'IBulkOperationTableNameProvider.ForRootEntity' override the table of the root entity only, but the owned type '{ownership.PrincipalEntityType.Name}.{ownership.PrincipalToDependent?.Name}' is mapped to the separate table '{entityType.GetTableName()}'. Use a 'TableNameProvider' that returns the table name of the entity and of every owned type that is mapped to a separate table.");

      return GetRootTableName(entityType);
   }

   /// <summary>
   /// Gets the target table name of the root entity type.
   /// </summary>
   protected abstract BulkOperationTableName GetRootTableName(IEntityType entityType);
}
