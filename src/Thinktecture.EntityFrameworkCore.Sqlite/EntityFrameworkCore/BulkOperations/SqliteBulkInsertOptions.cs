namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQLite.
/// </summary>
public sealed class SqliteBulkInsertOptions : IBulkInsertOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqliteBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqliteBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is null)
      {
         AutoIncrementBehavior = SqliteAutoIncrementBehavior.SetZeroToNull;
      }
      else
      {
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

         if (optionsToInitializeFrom is SqliteBulkInsertOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
