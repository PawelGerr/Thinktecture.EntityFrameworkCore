using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by the <see cref="SqliteDbContextExtensions.BulkInsertIntoTempTableAsync{T}"/>.
   /// </summary>
   public class SqliteTempTableBulkInsertOptions
   {
      /// <summary>
      /// Options for bulk insert.
      /// </summary>
      internal SqliteBulkInsertOptions BulkInsertOptions { get; }

      /// <summary>
      /// Options for creation of the temp table.
      /// Default is set to <c>true</c>.
      /// </summary>
      internal TempTableCreationOptions TempTableCreationOptions { get; }

      /// <summary>
      /// Properties to insert.
      /// If the <see cref="EntityMembersProvider"/> is null then all properties of the entity are going to be inserted.
      /// </summary>
      public IEntityMembersProvider? EntityMembersProvider
      {
         get => BulkInsertOptions.EntityMembersProvider;
         set => BulkInsertOptions.EntityMembersProvider = value;
      }

      /// <summary>
      /// Behavior for auto-increment columns.
      /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
      /// </summary>
      public SqliteAutoIncrementBehavior AutoIncrementBehavior
      {
         get => BulkInsertOptions.AutoIncrementBehavior;
         set => BulkInsertOptions.AutoIncrementBehavior = value;
      }

      /// <summary>
      /// Indication whether the table name should be unique.
      /// Default is <c>true</c>.
      /// </summary>
      public bool MakeTableNameUnique
      {
         get => TempTableCreationOptions.MakeTableNameUnique;
         set => TempTableCreationOptions.MakeTableNameUnique = value;
      }

      /// <summary>
      /// Indication whether to create the primary key along with the creation of the temp table.
      /// Default is <c>false</c>.
      /// </summary>
      public bool CreatePrimaryKey
      {
         get => TempTableCreationOptions.CreatePrimaryKey;
         set => TempTableCreationOptions.CreatePrimaryKey = value;
      }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteTempTableBulkInsertOptions"/>.
      /// </summary>
      public SqliteTempTableBulkInsertOptions()
      {
         BulkInsertOptions = new SqliteBulkInsertOptions();
         TempTableCreationOptions = new TempTableCreationOptions();
      }
   }
}
