using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for MySQL query-based bulk updates.
/// </summary>
[SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
public sealed class MySqlBulkUpdateFromQueryOptions : IBulkOperationFromQueryOptions
{
   private readonly BulkOperationTableNameOptions _tableNames = new();

   /// <inheritdoc />
   [Obsolete("Use 'TableNameProvider' instead: 'IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName)' when only 'TableName' is set, or 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)' when 'Schema' is set as well. For a temp table entity use 'IBulkOperationTableNameProvider.ForRootEntity(tableName, schema)', because 'ForRootEntityWithDefaultSchema' throws for temp table entities.")]
   public string? TableName
   {
      get => _tableNames.TableName;
      set => _tableNames.TableName = value;
   }

   /// <inheritdoc />
   /// <remarks>MySQL handles the schema according to the <c>SchemaBehavior</c> of <c>UseMySql</c>: <c>MySqlSchemaBehavior.Throw</c> (the default) rejects it, <c>Ignore</c> drops it, and <c>Translate</c> passes it to the configured translator together with the table name.</remarks>
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
}
