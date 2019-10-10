namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Defines whether and when the primary key should be created.
   /// </summary>
   public enum SqlServerPrimaryKeyCreation
   {
      /// <summary>
      /// No primary key should be created.
      /// </summary>
      None,

      /// <summary>
      /// Primary key should be created before bulk insert.
      /// </summary>
      BeforeBulkInsert,

      /// <summary>
      /// Primary key should be created after bulk insert.
      /// </summary>
      AfterBulkInsert
   }
}
