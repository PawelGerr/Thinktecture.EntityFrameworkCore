using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options for SQLite.
   /// </summary>
   public interface ISqliteTempTableBulkInsertOptions : ITempTableBulkInsertOptions
   {
      /// <summary>
      /// Behavior for auto-increment columns.
      /// Default is <see cref="SqliteAutoIncrementBehavior.SetZeroToNull"/>
      /// </summary>
      SqliteAutoIncrementBehavior AutoIncrementBehavior { get; set; }

      /// <summary>
      /// Drops/truncates the temp table if the table exists already.
      /// Default is <c>false</c>.
      /// </summary>
      bool TruncateTableIfExists { get; set; }

      /// <summary>
      /// Indication whether to drop the temp table on dispose of <see cref="ITempTableQuery{T}"/>.
      /// Default is <c>true</c>.
      /// </summary>
      /// <remarks>
      /// Set to <c>false</c> for more performance if the same temp table is re-used very often.
      /// Set <see cref="TruncateTableIfExists"/> to <c>true</c> on re-use.
      /// </remarks>
      bool DropTableOnDispose { get; set; }

      /// <summary>
      /// Provides the name to create a temp table with.
      /// </summary>

      ITempTableNameProvider TableNameProvider { get; set; }

      /// <summary>
      /// Provides the corresponding columns if the primary key should be created.
      /// The default is <see cref="PrimaryKeyPropertiesProviders.EntityTypeConfiguration"/>.
      /// </summary>
      IPrimaryKeyPropertiesProvider PrimaryKeyCreation { get; set; }

      /// <summary>
      /// Properties to insert.
      /// If the <see cref="PropertiesToInsert"/> is null then all properties of the entity are going to be inserted.
      /// </summary>
      IEntityPropertiesProvider? PropertiesToInsert { get; set; }
   }
}
