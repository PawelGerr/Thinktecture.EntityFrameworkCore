using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides a table name for the root entity type only and throws for owned entity types that are mapped to separate tables.
/// </summary>
internal abstract class RootEntityTableNameProviderBase : IBulkOperationTableNameProvider
{
   /// <summary>
   /// Builds the provider of the obsolete options 'TableName' and 'Schema'. A <c>null</c> part keeps the value of the EF Core model.
   /// The parts are not validated, so that the executor reports an empty part.
   /// </summary>
   /// <param name="tableName">Name of the target table.</param>
   /// <param name="schema">Schema of the target table.</param>
   /// <returns>A provider for the root entity type only, or <c>null</c> when both parts are <c>null</c>.</returns>
   public static IBulkOperationTableNameProvider? FromObsoleteOptions(string? tableName, string? schema)
   {
      return (tableName, schema) switch
      {
         (null, null) => null,
         (not null, null) => new RootEntityWithDefaultSchemaTableNameProvider(tableName, throwForTempTableEntity: false),
         (null, not null) => new RootEntityWithDefaultNameTableNameProvider(schema),
         _ => new RootEntityTableNameProvider(tableName, schema)
      };
   }

   public BulkOperationTableName GetTableName(IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      var ownership = entityType.FindOwnership();

      if (ownership is not null)
         throw new InvalidOperationException($"The options 'TableName' and 'Schema' and the factories 'IBulkOperationTableNameProvider.ForRootEntity', 'ForRootEntityWithDefaultSchema' and 'ForRootEntityWithDefaultName' override the table of the root entity only, but the owned type '{ownership.PrincipalEntityType.Name}.{ownership.PrincipalToDependent?.Name}' is mapped to the separate table '{entityType.GetTableName()}'. Use a 'TableNameProvider' that returns the table name of the entity and of every owned type that is mapped to a separate table.");

      return GetRootTableName(entityType);
   }

   /// <summary>
   /// Gets the target table name of the root entity type.
   /// </summary>
   protected abstract BulkOperationTableName GetRootTableName(IEntityType entityType);
}
