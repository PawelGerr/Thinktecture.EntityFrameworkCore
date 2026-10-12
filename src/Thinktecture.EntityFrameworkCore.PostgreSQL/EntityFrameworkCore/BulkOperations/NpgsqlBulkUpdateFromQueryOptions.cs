using System.Diagnostics.CodeAnalysis;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for query-based bulk update operations in PostgreSQL.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class NpgsqlBulkUpdateFromQueryOptions : IBulkOperationFromQueryOptions
{
   private readonly BulkOperationTableNameOptions _tableNames = new();

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
}
