using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert or update options for SQL Server.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class SqlServerBulkInsertOrUpdateOptions : ISqlServerMergeOperationOptions, IBulkInsertOrUpdateOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <inheritdoc />
   public List<SqlServerTableHintLimited> MergeTableHints { get; }

   /// <inheritdoc />
   public SqlServerBulkOperationTempTableOptions TempTableOptions { get; }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName)' when only 'TableName' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'Schema' is set as well.")]
   public string? TableName
   {
      get => _tableNames.TableName;
      set => _tableNames.TableName = value;
   }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(schema)' when only 'Schema' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'TableName' is set as well.")]
   public string? Schema
   {
      get => _tableNames.Schema;
      set => _tableNames.Schema = value;
   }

   /// <inheritdoc />
   public IBulkOperationTableNameProvider TableNameProvider
   {
      get => _tableNames.TableNameProvider;
      set => _tableNames.TableNameProvider = value;
   }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="holdLockDuringMerge">Indication, whether the table hint <see cref="SqlServerTableHintLimited.HoldLock"/> should be used with 'MERGE' or not.</param>
   public SqlServerBulkInsertOrUpdateOptions(bool holdLockDuringMerge)
      : this(null, holdLockDuringMerge)
   {
   }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
      : this(optionsToInitializeFrom, optionsToInitializeFrom is null)
   {
   }

   private SqlServerBulkInsertOrUpdateOptions(
      IBulkInsertOrUpdateOptions? optionsToInitializeFrom,
      bool holdLockDuringMerge)
   {
#pragma warning disable CS0618 // TableName and Schema are obsolete but must be copied for backward compatibility.
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618

      if (optionsToInitializeFrom is not null)
      {
         PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
      }

      if (optionsToInitializeFrom is ISqlServerMergeOperationOptions mergeOptions)
      {
         TempTableOptions = new SqlServerBulkOperationTempTableOptions(mergeOptions.TempTableOptions);
         MergeTableHints = mergeOptions.MergeTableHints.ToList();

         if (holdLockDuringMerge && !MergeTableHints.Contains(SqlServerTableHintLimited.HoldLock))
            MergeTableHints.Add(SqlServerTableHintLimited.HoldLock);
      }
      else
      {
         TempTableOptions = new SqlServerBulkOperationTempTableOptions
                            {
                               SqlBulkCopyOptions = SqlBulkCopyOptions.KeepIdentity
                            };
         MergeTableHints = new List<SqlServerTableHintLimited>();

         if (holdLockDuringMerge)
            MergeTableHints.Add(SqlServerTableHintLimited.HoldLock);
      }
   }

   /// <summary>
   /// Gets the options for bulk insert into a temp table.
   /// </summary>
   public SqlServerTempTableBulkOperationOptions GetTempTableBulkInsertOptions()
   {
      var options = new SqlServerTempTableBulkInsertOptions
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
