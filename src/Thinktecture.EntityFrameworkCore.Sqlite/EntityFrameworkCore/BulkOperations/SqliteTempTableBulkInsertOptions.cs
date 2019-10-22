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
      /// Indication whether the table name should be unique.
      /// Default is <c>true</c>.
      /// </summary>
      public bool MakeTableNameUnique
      {
         get => _tempTableCreationOptions.MakeTableNameUnique;
         set => _tempTableCreationOptions.MakeTableNameUnique = value;
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
      /// If the <see cref="EntityMembersProvider"/> is null then all properties of the entity are going to be inserted.
      /// </summary>
      public IEntityMembersProvider? EntityMembersProvider
      {
         get => _bulkInsertOptions.EntityMembersProvider;
         set
         {
            _bulkInsertOptions.EntityMembersProvider = value;
            _tempTableCreationOptions.EntityMembersProvider = value;
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
         EntityMembersProvider = options.BulkInsertOptions.EntityMembersProvider;
         MakeTableNameUnique = options.TempTableCreationOptions.MakeTableNameUnique;
         CreatePrimaryKey = options.TempTableCreationOptions.CreatePrimaryKey;

         if (options is SqliteTempTableBulkInsertOptions sqliteOptions)
            AutoIncrementBehavior = sqliteOptions.AutoIncrementBehavior;
      }
   }
}
