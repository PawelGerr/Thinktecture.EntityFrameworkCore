using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides the table name of <see cref="IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema"/>.
/// The schema comes from the EF Core model.
/// Throws for temp table entities when <paramref name="throwForTempTableEntity"/> is <c>true</c>; the obsolete option 'TableName' keeps the model schema of a temp table entity.
/// </summary>
/// <param name="tableName">Name of the target table.</param>
/// <param name="throwForTempTableEntity">Indication whether to throw for temp table entities.</param>
internal sealed class RootEntityWithDefaultSchemaTableNameProvider(string tableName, bool throwForTempTableEntity) : RootEntityTableNameProviderBase
{
   protected override BulkOperationTableName GetRootTableName(IEntityType entityType)
   {
      if (throwForTempTableEntity && entityType.IsTempTableEntityType())
         throw new InvalidOperationException($"The entity type '{entityType.ClrType.ShortDisplayName()}' is configured as a temp table entity and has no regular table, so the schema of the EF Core model is not defined. Use 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' instead of 'ForRootEntityWithDefaultSchema'.");

      return new BulkOperationTableName(tableName, entityType.GetSchema());
   }
}
