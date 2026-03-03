using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class BulkOperationModelExtensions
{
   /// <summary>
   /// Resolves the entity type, table name, and schema for the given type <typeparamref name="T"/>.
   /// Falls back to temp table entity lookup if the type is not found as a regular entity.
   /// When a temp table entity is found, the caller must provide <paramref name="tableNameOverride"/>.
   /// </summary>
   public static (IEntityType EntityType, string TableName, string? Schema) GetEntityType<T>(
      this IModel model,
      string? tableNameOverride,
      string? schemaOverride)
      where T : class
   {
      var entityType = model.FindEntityType(typeof(T));

      if (entityType is null)
      {
         entityType = model.FindEntityType(EntityNameProvider.GetTempTableName(typeof(T)));

         if (entityType is null)
            throw new EntityTypeNotFoundException(typeof(T));

         if (tableNameOverride is null)
            throw new InvalidOperationException($"The entity type for '{typeof(T).ShortDisplayName()}' is configured as a temp table entity and does not have a regular table mapping. Provide the target table name via the 'TableName' property in options.");
      }

      var tableName = tableNameOverride
                      ?? entityType.GetTableName()
                      ?? throw new InvalidOperationException($"The entity '{entityType.Name}' has no table name.");
      var schema = schemaOverride ?? entityType.GetSchema();

      return (entityType, tableName, schema);
   }
}
