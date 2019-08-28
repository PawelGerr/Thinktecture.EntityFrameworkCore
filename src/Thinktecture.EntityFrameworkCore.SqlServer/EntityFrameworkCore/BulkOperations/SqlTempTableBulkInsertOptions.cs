using JetBrains.Annotations;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by the <see cref="IBulkOperationExecutor"/>.
   /// </summary>
   public class SqlTempTableBulkInsertOptions
   {
      private SqlServerBulkInsertOptions _serverBulkInsertOptions;

      /// <summary>
      /// Options for bulk insert.
      /// </summary>
      [NotNull]
      public SqlServerBulkInsertOptions ServerBulkInsertOptions
      {
         get => _serverBulkInsertOptions ?? (_serverBulkInsertOptions = new SqlServerBulkInsertOptions());
         set => _serverBulkInsertOptions = value;
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
