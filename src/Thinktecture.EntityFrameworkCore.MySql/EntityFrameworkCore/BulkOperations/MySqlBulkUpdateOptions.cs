using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// MySQL bulk update options.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class MySqlBulkUpdateOptions : IBulkUpdateOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToUpdate { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? KeyProperties { get; set; }

   /// <summary>
   /// Maximum number of rows per INSERT statement into the staging temp table. The default is 500.
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
   /// Initializes a new instance of <see cref="MySqlBulkUpdateOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlBulkUpdateOptions(IBulkUpdateOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618
      PropertiesToUpdate = optionsToInitializeFrom?.PropertiesToUpdate;
      KeyProperties = optionsToInitializeFrom?.KeyProperties;

      if (optionsToInitializeFrom is MySqlBulkUpdateOptions mySqlOptions)
      {
         BatchSize = mySqlOptions.BatchSize;
         CommandTimeout = mySqlOptions.CommandTimeout;
      }
   }
}