namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options for SQL Server.
/// </summary>
public sealed class SqlServerBulkUpdateOptions : SqlServerBulkOperationOptions, IBulkUpdateOptions
{
   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom as SqlServerBulkOperationOptions,
             optionsToInitializeFrom?.PropertiesToUpdate,
             optionsToInitializeFrom?.KeyProperties)
   {
   }

   /// <summary>
   /// Gets the options for bulk insert into a temp table.
   /// </summary>
   public SqlServerTempTableBulkOperationOptions GetTempTableBulkInsertOptions()
   {
      var options = new SqlServerTempTableBulkInsertOptions
                    {
                       PropertiesToInsert = PropertiesToUpdate is null ? null : new CompositeTempTableEntityPropertiesProvider(null, PropertiesToUpdate, KeyProperties),
                       Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                    };

      TempTableOptions.Populate(options);

      return options;
   }
}
