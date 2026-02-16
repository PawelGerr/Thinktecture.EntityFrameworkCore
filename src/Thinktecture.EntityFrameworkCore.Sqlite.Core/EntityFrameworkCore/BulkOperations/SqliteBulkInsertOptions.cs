namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQLite.
/// </summary>
public sealed class SqliteBulkInsertOptions : IBulkInsertOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   public string? TableName { get; set; }

   /// <inheritdoc />
   public string? Schema { get; set; }

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
      AutoIncrementBehavior = SqliteAutoIncrementBehavior.SetZeroToNull;

      if (optionsToInitializeFrom is not null)
      {
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;
         TableName = optionsToInitializeFrom.TableName;
         Schema = optionsToInitializeFrom.Schema;

         if (optionsToInitializeFrom is SqliteBulkInsertOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
