namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for the query-based bulk insert operation on PostgreSQL.
/// </summary>
public sealed class NpgsqlBulkInsertFromQueryOptions
{
   /// <summary>
   /// Overrides the target table name resolved from the entity type.
   /// If <c>null</c>, the table name is resolved from the EF Core model metadata.
   /// </summary>
   public string? TableName { get; set; }

   /// <summary>
   /// Overrides the target schema resolved from the entity type.
   /// If <c>null</c>, the schema is resolved from the EF Core model metadata.
   /// </summary>
   public string? Schema { get; set; }
}
