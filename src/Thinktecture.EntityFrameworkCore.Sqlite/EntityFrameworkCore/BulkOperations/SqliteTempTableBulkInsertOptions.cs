using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQLite.
   /// </summary>
   public sealed class SqliteTempTableBulkInsertOptions : ITempTableBulkInsertOptions
   {
      IBulkInsertOptions ITempTableBulkInsertOptions.BulkInsertOptions => _bulkInsertOptions;
      ITempTableCreationOptions ITempTableBulkInsertOptions.TempTableCreationOptions => _tempTableCreationOptions;

      private readonly SqliteBulkInsertOptions _bulkInsertOptions;
      private readonly TempTableCreationOptions _tempTableCreationOptions;

      /// <summary>
      /// Behavior for auto-increment columns.
      /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
      /// </summary>
      public SqliteAutoIncrementBehavior AutoIncrementBehavior
      {
         get => _bulkInsertOptions.AutoIncrementBehavior;
         set => _bulkInsertOptions.AutoIncrementBehavior = value;
      }

      /// <summary>
      /// Drops/truncates the temp table if the table exists already.
      /// Default is <c>false</c>.
      /// </summary>
      public bool DropTempTableIfExists
      {
         get => _tempTableCreationOptions.DropTempTableIfExists;
         set => _tempTableCreationOptions.DropTempTableIfExists = value;
      }

      /// <summary>
      /// Provides the name to create a temp table with.
      /// </summary>

      public ITempTableNameProvider TableNameProvider
      {
         get => _tempTableCreationOptions.TableNameProvider;
         set => _tempTableCreationOptions.TableNameProvider = value;
      }

      /// <summary>
      /// Indication whether to create the primary key along with the creation of the temp table.
      /// Default is <c>false</c>.
      /// </summary>
      public bool CreatePrimaryKey
      {
         get => _tempTableCreationOptions.CreatePrimaryKey;
         set => _tempTableCreationOptions.CreatePrimaryKey = value;
      }

      /// <summary>
      /// Properties to insert.
      /// If the <see cref="MembersToInsert"/> is null then all properties of the entity are going to be inserted.
      /// </summary>
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
         _bulkInsertOptions = new SqliteBulkInsertOptions();
         _tempTableCreationOptions = new TempTableCreationOptions();

         if (optionsToInitializeFrom != null)
            InitializeFrom(optionsToInitializeFrom);
      }

      private void InitializeFrom(ITempTableBulkInsertOptions options)
      {
         MembersToInsert = options.BulkInsertOptions.MembersToInsert;
         TableNameProvider = options.TempTableCreationOptions.TableNameProvider;
         CreatePrimaryKey = options.TempTableCreationOptions.CreatePrimaryKey;
         DropTempTableIfExists = options.TempTableCreationOptions.DropTempTableIfExists;

         if (options is SqliteTempTableBulkInsertOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
