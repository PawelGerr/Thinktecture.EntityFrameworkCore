using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class BulkOperationTableNameProviderExtensions
{
   /// <summary>
   /// Gets the table name of <paramref name="entityType"/> from <paramref name="tableNameProvider"/> and validates it.
   /// </summary>
   /// <param name="tableNameProvider">The provider of the table names.</param>
   /// <param name="entityType">The entity type to get the table name for.</param>
   /// <returns>The validated table name.</returns>
   /// <exception cref="InvalidOperationException">The provider returned no table name or an empty schema.</exception>
   public static BulkOperationTableName GetValidTableName(this IBulkOperationTableNameProvider tableNameProvider, IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(tableNameProvider);
      ArgumentNullException.ThrowIfNull(entityType);

      var tableName = tableNameProvider.GetTableName(entityType);

      if (String.IsNullOrWhiteSpace(tableName.Name))
         throw new InvalidOperationException($"The 'TableNameProvider' returned no table name for the entity type '{entityType.Name}'.");

      if (tableName.Schema is not null && String.IsNullOrWhiteSpace(tableName.Schema))
         throw new InvalidOperationException($"The 'TableNameProvider' returned an empty schema for the entity type '{entityType.Name}'. Return null to use the default schema of the database.");

      return tableName;
   }
}
