using JetBrains.Annotations;
using Thinktecture.EntityFrameworkCore.TempTables;

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

      private TempTableCreationOptions _tempTableCreationOptions;

      /// <summary>
      /// Options for creation of the temp table.
      /// Default is set to <c>true</c>.
      /// </summary>
      [NotNull]
      public TempTableCreationOptions TempTableCreationOptions
      {
         get => _tempTableCreationOptions ?? (_tempTableCreationOptions = new TempTableCreationOptions());
         set => _tempTableCreationOptions = value;
      }

      /// <summary>
      /// Creates a clustered primary key spanning all columns of the temp table after the bulk insert.
      /// Default is set to <c>true</c>.
      /// </summary>
      public PrimaryKeyCreation PrimaryKeyCreation { get; set; } = PrimaryKeyCreation.AfterBulkInsert;
   }
}
