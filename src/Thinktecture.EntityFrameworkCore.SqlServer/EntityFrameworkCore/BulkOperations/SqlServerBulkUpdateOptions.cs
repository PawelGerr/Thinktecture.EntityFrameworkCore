namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options for SQL Server.
/// </summary>
public sealed class SqlServerBulkUpdateOptions : ISqlServerMergeOperationOptions, IBulkUpdateOptions
{
   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <inheritdoc />
   public List<SqlServerTableHintLimited> MergeTableHints { get; }

   /// <inheritdoc />
   public SqlServerBulkOperationTempTableOptions TempTableOptions { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
   {
      if (optionsToInitializeFrom is not null)
      {
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
      }

      if (optionsToInitializeFrom is ISqlServerMergeOperationOptions mergeOptions)
      {
         TempTableOptions = new SqlServerBulkOperationTempTableOptions(mergeOptions.TempTableOptions);
         MergeTableHints = mergeOptions.MergeTableHints.ToList();
      }
      else
      {
         TempTableOptions = new SqlServerBulkOperationTempTableOptions();
         MergeTableHints = new List<SqlServerTableHintLimited> { SqlServerTableHintLimited.HoldLock };
      }
   }

   /// <summary>
   /// Gets the options for bulk insert into a temp table.
   /// </summary>
   public SqlServerTempTableBulkOperationOptions GetTempTableBulkInsertOptions()
   {
      var options = new SqlServerTempTableBulkInsertOptions
                    {
                       PropertiesToInsert = PropertiesToUpdate is null ? null : CompositeTempTableEntityPropertiesProvider.CreateForUpdate(PropertiesToUpdate, KeyProperties),
                       Advanced = { UsePropertiesToInsertForTempTableCreation = true }
                    };

      TempTableOptions.Populate(options);

      return options;
   }
}
