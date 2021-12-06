namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk insert options for SQLite.
/// </summary>
public interface ISqliteBulkInsertOptions : IBulkInsertOptions
{
   /// <summary>
   /// Behavior for auto-increment columns.
   /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
   /// </summary>
   SqliteAutoIncrementBehavior AutoIncrementBehavior { get; }
}