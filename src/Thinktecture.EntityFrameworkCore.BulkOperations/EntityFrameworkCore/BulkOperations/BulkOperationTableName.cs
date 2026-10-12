namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Name of a target table of a bulk operation.
/// </summary>
/// <param name="Name">Name of the table. Must not be empty.</param>
/// <param name="Schema">Schema of the table. Must be <c>null</c> or non-empty. <c>null</c> means that no schema is emitted, so the database uses its default schema.</param>
public readonly record struct BulkOperationTableName(string Name, string? Schema = null);
