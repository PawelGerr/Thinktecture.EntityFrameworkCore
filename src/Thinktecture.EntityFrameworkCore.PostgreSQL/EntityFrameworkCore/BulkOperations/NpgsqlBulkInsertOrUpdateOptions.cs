using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert or update options for PostgreSQL.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class NpgsqlBulkInsertOrUpdateOptions : IBulkInsertOrUpdateOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

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
   /// Initializes new instance of <see cref="NpgsqlBulkInsertOrUpdateOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
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
