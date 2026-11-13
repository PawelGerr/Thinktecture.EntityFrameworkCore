using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// MySQL bulk insert options.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class MySqlBulkInsertOptions : IBulkInsertOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <summary>
   /// Maximum number of rows per INSERT statement. The default is 500; the value is reduced to stay below the parameter limit of 65,535. Ignored when <see cref="UseBulkCopy" /> is enabled.
   /// </summary>
   public int? BatchSize { get; set; }

   /// <summary>
   /// Timeout of the generated commands. <c>null</c> uses the default of the connection.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <summary>
   /// Uses <see cref="MySqlConnector.MySqlBulkCopy" /> (<c>LOAD DATA LOCAL INFILE</c>) instead of multi-row INSERT.
   /// The connection must enable <c>AllowLoadLocalInfile=true</c>, and the server must enable <c>local_infile</c>.
   /// This path buffers the input in memory before copying it.
   /// </summary>
   public bool UseBulkCopy { get; set; }

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
   /// Initializes a new instance of <see cref="MySqlBulkInsertOptions" />.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to copy.</param>
   public MySqlBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618
      PropertiesToInsert = optionsToInitializeFrom?.PropertiesToInsert;

      if (optionsToInitializeFrom is MySqlBulkInsertOptions mySqlOptions)
      {
         BatchSize = mySqlOptions.BatchSize;
         CommandTimeout = mySqlOptions.CommandTimeout;
         UseBulkCopy = mySqlOptions.UseBulkCopy;
      }
   }
}