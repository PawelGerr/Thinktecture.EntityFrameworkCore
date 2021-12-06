namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for SQLite.
/// </summary>
public interface ISqliteBulkInsertOrUpdateOptions : IBulkInsertOrUpdateOptions
{
   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   SqliteAutoIncrementBehavior AutoIncrementBehavior { get; }
}