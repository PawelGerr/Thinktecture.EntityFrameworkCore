namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert or update options for PostgreSQL.
/// </summary>
public sealed class NpgsqlBulkInsertOrUpdateOptions : IBulkInsertOrUpdateOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// If <c>true</c>, uses <c>ON CONFLICT ... DO NOTHING</c> instead of <c>ON CONFLICT ... DO UPDATE SET</c>.
   /// Conflicting rows are silently skipped rather than updated.
   /// Default is <c>false</c>.
   /// </summary>
   public bool ConflictDoNothing { get; set; }

   /// <summary>
   /// Temp table options for the bulk insert-or-update operation.
   /// </summary>
   public NpgsqlBulkOperationTempTableOptions TempTableOptions { get; }

   /// <inheritdoc />
   public string? TableName { get; set; }

   /// <inheritdoc />
   public string? Schema { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlBulkInsertOrUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is not null)
      {
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
         TableName = optionsToInitializeFrom.TableName;
         Schema = optionsToInitializeFrom.Schema;
      }

      if (optionsToInitializeFrom is NpgsqlBulkInsertOrUpdateOptions npgsqlOptions)
      {
         ConflictDoNothing = npgsqlOptions.ConflictDoNothing;
         TempTableOptions = new NpgsqlBulkOperationTempTableOptions(npgsqlOptions.TempTableOptions);
      }
      else
      {
         TempTableOptions = new NpgsqlBulkOperationTempTableOptions();
      }
   }

   /// <summary>
   /// Gets the options for bulk insert into a temp table.
   /// </summary>
   public NpgsqlTempTableBulkInsertOptions GetTempTableBulkInsertOptions()
   {
      var options = new NpgsqlTempTableBulkInsertOptions
                    {
                       PropertiesToInsert = PropertiesToInsert is null || PropertiesToUpdate is null
                                               ? null
                                               : CompositeTempTableEntityPropertiesProvider.CreateForInsertOrUpdate(PropertiesToInsert, PropertiesToUpdate, KeyProperties),
                       Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                    };

      TempTableOptions.Populate(options);

      return options;
   }
}
