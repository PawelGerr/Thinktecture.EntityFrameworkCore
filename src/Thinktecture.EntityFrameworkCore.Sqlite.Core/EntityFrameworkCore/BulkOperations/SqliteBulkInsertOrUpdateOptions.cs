namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for SQLite.
/// </summary>
public class SqliteBulkInsertOrUpdateOptions : ISqliteBulkInsertOrUpdateOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <inheritdoc />
   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; }

   /// <inheritdoc />
   public string? TableName { get; set; }

   /// <inheritdoc />
   public string? Schema { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteBulkInsertOrUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqliteBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
   {
      AutoIncrementBehavior = SqliteAutoIncrementBehavior.SetZeroToNull;

      if (optionsToInitializeFrom is not null)
      {
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
         TableName = optionsToInitializeFrom.TableName;
         Schema = optionsToInitializeFrom.Schema;

         if (optionsToInitializeFrom is ISqliteBulkInsertOrUpdateOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
