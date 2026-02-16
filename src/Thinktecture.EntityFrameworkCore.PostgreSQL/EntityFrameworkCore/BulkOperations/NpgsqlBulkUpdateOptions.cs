namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options for PostgreSQL.
/// </summary>
public sealed class NpgsqlBulkUpdateOptions : IBulkUpdateOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Temp table options for the bulk update operation.
   /// </summary>
   public NpgsqlBulkOperationTempTableOptions TempTableOptions { get; }

   /// <inheritdoc />
   public string? TableName { get; set; }

   /// <inheritdoc />
   public string? Schema { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is not null)
      {
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
         TableName = optionsToInitializeFrom.TableName;
         Schema = optionsToInitializeFrom.Schema;
      }

      if (optionsToInitializeFrom is NpgsqlBulkUpdateOptions npgsqlOptions)
      {
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
                       PropertiesToInsert = PropertiesToUpdate is null ? null : CompositeTempTableEntityPropertiesProvider.CreateForUpdate(PropertiesToUpdate, KeyProperties),
                       Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                    };

      TempTableOptions.Populate(options);

      return options;
   }
}
