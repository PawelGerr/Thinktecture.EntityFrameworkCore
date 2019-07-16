using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by the <see cref="ISqlServerBulkOperationExecutor"/>.
   /// </summary>
   public class SqlTempTableBulkInsertOptions
   {
      private SqlBulkInsertOptions _bulkInsertOptions;

      /// <summary>
      /// Options for bulk insert.
      /// </summary>
      [NotNull]
      public SqlBulkInsertOptions BulkInsertOptions
      {
         get => _bulkInsertOptions ?? (_bulkInsertOptions = new SqlBulkInsertOptions());
         set => _bulkInsertOptions = value;
      }

      /// <summary>
      /// Indication whether the name of the temp table should be unique.
      /// Default is set to <c>true</c>.
      /// </summary>
      public bool MakeTableNameUnique { get; set; } = true;

      /// <summary>
      /// Creates a clustered primary key spanning all columns of the temp table after the bulk insert.
      /// Default is set to <c>true</c>.
      /// </summary>
      public PrimaryKeyCreation PrimaryKeyCreation { get; set; } = PrimaryKeyCreation.AfterBulkInsert;
   }
}
