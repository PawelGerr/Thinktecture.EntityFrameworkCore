using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// MySQL upsert options. MySQL fires <c>ON DUPLICATE KEY</c> on any primary or unique index conflict, so the update
/// is guarded by the key properties: only a row whose key columns equal the incoming key is updated; key columns are never
/// updated. An incoming row that collides with a different row on another unique index is skipped silently.
/// The key properties must be part of the properties to insert. The update uses the inserted values of the incoming row, so
/// <see cref="PropertiesToUpdate"/> must be a subset of the properties to insert. If <see cref="PropertiesToUpdate"/> is not set,
/// the inserted properties (except the keys) are updated.
/// The result is the number of processed input rows (MySQL's own affected-row counts differ for inserted, updated and unchanged rows).
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class MySqlBulkInsertOrUpdateOptions : IBulkInsertOrUpdateOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Maximum number of rows per statement. The default is 500; the value is reduced to stay below the parameter limit of 65,535.
   /// </summary>
   public int? BatchSize { get; set; }

   /// <summary>
   /// Timeout of the generated commands. <c>null</c> uses the default of the connection.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead.")]
   public string? TableName
   {
      get => _tableNames.TableName;
      set => _tableNames.TableName = value;
   }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead.")]
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
   /// Initializes a new instance of <see cref="MySqlBulkInsertOrUpdateOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlBulkInsertOrUpdateOptions(IBulkInsertOrUpdateOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618
      PropertiesToInsert = optionsToInitializeFrom?.PropertiesToInsert;
      PropertiesToUpdate = optionsToInitializeFrom?.PropertiesToUpdate;
      KeyProperties = optionsToInitializeFrom?.KeyProperties;

      if (optionsToInitializeFrom is MySqlBulkInsertOrUpdateOptions mySqlOptions)
      {
         BatchSize = mySqlOptions.BatchSize;
         CommandTimeout = mySqlOptions.CommandTimeout;
      }
   }
}