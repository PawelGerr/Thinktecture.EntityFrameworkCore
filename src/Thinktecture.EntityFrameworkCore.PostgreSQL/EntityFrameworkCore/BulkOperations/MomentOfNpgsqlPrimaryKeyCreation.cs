namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Defines when the primary key should be created.
/// </summary>
public enum MomentOfNpgsqlPrimaryKeyCreation
{
   /// <summary>
   /// Primary key should be created before bulk insert.
   /// </summary>
   BeforeBulkInsert,

   /// <summary>
   /// Primary key should be created after bulk insert.
   /// </summary>
   AfterBulkInsert
}
