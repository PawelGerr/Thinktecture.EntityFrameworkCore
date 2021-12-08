namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert or update options for SQL Server.
/// </summary>
public sealed class SqlServerBulkInsertOrUpdateOptions
   : SqlServerBulkOperationOptions, IBulkInsertOrUpdateOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom as SqlServerBulkOperationOptions,
             optionsToInitializeFrom?.PropertiesToUpdate,
             optionsToInitializeFrom?.KeyProperties)
   {
      PropertiesToInsert = optionsToInitializeFrom?.PropertiesToInsert;
   }

   /// <summary>
   /// Gets the options for bulk insert into a temp table.
   /// </summary>
   public SqlServerTempTableBulkOperationOptions GetTempTableBulkInsertOptions()
   {
      var options = new SqlServerTempTableBulkInsertOptions
                    {
                       PropertiesToInsert = PropertiesToInsert is null && PropertiesToUpdate is null
                                               ? null
                                               : new CompositeTempTableEntityPropertiesProvider(PropertiesToInsert, PropertiesToUpdate, KeyProperties),
                       Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                    };

      TempTableOptions.Populate(options);

      return options;
   }
}
