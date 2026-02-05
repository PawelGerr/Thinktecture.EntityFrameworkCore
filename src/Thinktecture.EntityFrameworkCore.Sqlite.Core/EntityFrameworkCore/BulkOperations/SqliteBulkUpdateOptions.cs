namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options for SQLite.
/// </summary>
public class SqliteBulkUpdateOptions : IBulkUpdateOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqliteBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
   {
      AutoIncrementBehavior = SqliteAutoIncrementBehavior.SetZeroToNull;

      if (optionsToInitializeFrom is not null)
      {
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;

         if (optionsToInitializeFrom is SqliteBulkUpdateOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
