using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQLite.
   /// </summary>
   public sealed class SqliteTempTableBulkInsertOptions : ISqliteTempTableBulkInsertOptions
   {
      IBulkInsertOptions ITempTableBulkInsertOptions.BulkInsertOptions => _bulkInsertOptions;
      ITempTableCreationOptions ITempTableBulkInsertOptions.TempTableCreationOptions => _tempTableCreationOptions;

      private readonly SqliteBulkInsertOptions _bulkInsertOptions;
      private readonly TempTableCreationOptions _tempTableCreationOptions;

      /// <inheritdoc />
      public SqliteAutoIncrementBehavior AutoIncrementBehavior
      {
         get => _bulkInsertOptions.AutoIncrementBehavior;
         set => _bulkInsertOptions.AutoIncrementBehavior = value;
      }

      /// <inheritdoc />
      public bool TruncateTableIfExists
      {
         get => _tempTableCreationOptions.TruncateTableIfExists;
         set => _tempTableCreationOptions.TruncateTableIfExists = value;
      }

      /// <inheritdoc />
      public bool DropTableOnDispose
      {
         get => _tempTableCreationOptions.DropTableOnDispose;
         set => _tempTableCreationOptions.DropTableOnDispose = value;
      }

      /// <inheritdoc />
      public ITempTableNameProvider TableNameProvider
      {
         get => _tempTableCreationOptions.TableNameProvider;
         set => _tempTableCreationOptions.TableNameProvider = value;
      }

      /// <inheritdoc />
      public IPrimaryKeyPropertiesProvider PrimaryKeyCreation
      {
         get => _tempTableCreationOptions.PrimaryKeyCreation;
         set => _tempTableCreationOptions.PrimaryKeyCreation = value;
      }

      /// <inheritdoc />
      public IEntityMembersProvider? MembersToInsert
      {
         get => _bulkInsertOptions.MembersToInsert;
         set
         {
            _bulkInsertOptions.MembersToInsert = value;
            _tempTableCreationOptions.MembersToInclude = value;
         }
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteTempTableBulkInsertOptions"/>.
      /// </summary>
      public SqliteTempTableBulkInsertOptions(ITempTableBulkInsertOptions? optionsToInitializeFrom = null)
      {
         _bulkInsertOptions = new SqliteBulkInsertOptions(optionsToInitializeFrom?.BulkInsertOptions);
         _tempTableCreationOptions = new TempTableCreationOptions(optionsToInitializeFrom?.TempTableCreationOptions);
      }
   }
}
