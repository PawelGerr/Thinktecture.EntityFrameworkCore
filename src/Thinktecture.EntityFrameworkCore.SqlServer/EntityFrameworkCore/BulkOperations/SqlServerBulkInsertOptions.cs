using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQL Server.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class SqlServerBulkInsertOptions : IBulkInsertOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <summary>
   /// Timeout used by <see cref="SqlBulkCopy"/>
   /// </summary>
   public TimeSpan? BulkCopyTimeout { get; set; }

   /// <summary>
   /// Options used by <see cref="SqlBulkCopy"/>.
   /// </summary>
   public SqlBulkCopyOptions SqlBulkCopyOptions { get; set; }

   /// <summary>
   /// Batch size used by <see cref="SqlBulkCopy"/>.
   /// </summary>
   public int? BatchSize { get; set; }

   /// <summary>
   /// Enables or disables a <see cref="SqlBulkCopy"/> object to stream data from an <see cref="IDataReader"/> object.
   /// Default is set to <c>true</c>.
   /// </summary>
   public bool EnableStreaming { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

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
   /// Initializes new instance of <see cref="SqlServerBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public SqlServerBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618 // TableName and Schema are obsolete but must be copied for backward compatibility.
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618

      EnableStreaming = true;

      if (optionsToInitializeFrom == null)
         return;

      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

      if (optionsToInitializeFrom is SqlServerBulkInsertOptions sqlServerOptions)
      {
         BulkCopyTimeout = sqlServerOptions.BulkCopyTimeout;
         SqlBulkCopyOptions = sqlServerOptions.SqlBulkCopyOptions;
         BatchSize = sqlServerOptions.BatchSize;
         EnableStreaming = sqlServerOptions.EnableStreaming;
      }
   }
}
