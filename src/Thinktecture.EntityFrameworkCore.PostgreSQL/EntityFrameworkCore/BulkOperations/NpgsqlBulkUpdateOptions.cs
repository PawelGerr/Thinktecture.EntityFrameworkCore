using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk update options for PostgreSQL.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class NpgsqlBulkUpdateOptions : IBulkUpdateOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Temp table options for the bulk update operation.
   /// </summary>
   public NpgsqlBulkOperationTempTableOptions TempTableOptions { get; }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName)' when only 'TableName' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'Schema' is set as well. For a temp table entity use 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)', because 'ForRootEntityWithDefaultSchema' throws for temp table entities.")]
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
   /// Initializes new instance of <see cref="NpgsqlBulkUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618 // TableName and Schema are obsolete but must be copied for backward compatibility.
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618

      if (optionsToInitializeFrom is not null)
      {
         PropertiesToUpdate = optionsToInitializeFrom.PropertiesToUpdate;
         KeyProperties = optionsToInitializeFrom.KeyProperties;
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
