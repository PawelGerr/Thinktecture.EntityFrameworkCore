namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert or update options for SQL Server.
/// </summary>
public sealed class SqlServerBulkInsertOrUpdateOptions
   : SqlServerBulkOperationOptions, ISqlServerBulkInsertOrUpdateOptions
{
   ISqlServerTempTableBulkInsertOptions ISqlServerBulkOperationOptions.TempTableOptions => TempTableOptions;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
      : base(optionsToInitializeFrom as ISqlServerBulkOperationOptions,
             optionsToInitializeFrom?.PropertiesToUpdate,
             optionsToInitializeFrom?.KeyProperties)
   {
      PropertiesToInsert = optionsToInitializeFrom?.PropertiesToInsert;
   }
}