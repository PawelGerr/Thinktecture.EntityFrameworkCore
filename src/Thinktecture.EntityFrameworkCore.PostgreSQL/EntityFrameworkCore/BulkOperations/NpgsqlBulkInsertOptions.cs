using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for PostgreSQL.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class NpgsqlBulkInsertOptions : IBulkInsertOptions
{
   private readonly BulkOperationTableNameOptions _tableNames;

   /// <summary>
   /// Timeout for the COPY command.
   /// </summary>
   public TimeSpan? CommandTimeout { get; set; }

   /// <summary>
   /// If <c>true</c>, appends the <c>FREEZE</c> option to the <c>COPY</c> command.
   /// Rows are frozen immediately, bypassing the normal MVCC visibility checks.
   /// This can significantly improve performance for bulk loads into tables that were created or truncated
   /// in the current transaction with no other concurrent readers.
   /// Default is <c>false</c>.
   /// </summary>
   public bool Freeze { get; set; }

   /// <inheritdoc />
   public IEntityPropertiesProvider? PropertiesToInsert { get; set; }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'. Note: 'ForRootEntity' emits no schema when 'schema' is null, while 'TableName' alone keeps the schema of the EF Core model.")]
   public string? TableName
   {
      get => _tableNames.TableName;
      set => _tableNames.TableName = value;
   }

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead, for example 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)'.")]
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
   /// Initializes new instance of <see cref="NpgsqlBulkInsertOptions"/>.
   /// </summary>
   /// <param name="optionsToInitializeFrom">Options to initialize from.</param>
   public NpgsqlBulkInsertOptions(IBulkInsertOptions? optionsToInitializeFrom = null)
   {
#pragma warning disable CS0618 // TableName and Schema are obsolete but must be copied for backward compatibility.
      _tableNames = new BulkOperationTableNameOptions(optionsToInitializeFrom?.TableName, optionsToInitializeFrom?.Schema, optionsToInitializeFrom?.TableNameProvider);
#pragma warning restore CS0618

      if (optionsToInitializeFrom is null)
         return;

      PropertiesToInsert = optionsToInitializeFrom.PropertiesToInsert;

      if (optionsToInitializeFrom is NpgsqlBulkInsertOptions npgsqlOptions)
      {
         CommandTimeout = npgsqlOptions.CommandTimeout;
         Freeze = npgsqlOptions.Freeze;
      }
   }
}
