using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Options for bulk insert into a temp table.
/// </summary>
public interface ITempTableBulkInsertOptions
{
   /// <summary>
   /// Drops/truncates the temp table if the table exists already.
   /// Default is <c>false</c>.
   /// </summary>
   bool TruncateTableIfExists { get; }

   /// <summary>
   /// Indication whether to drop the temp table on dispose of <see cref="ITempTableQuery{T}"/>.
   /// Default is <c>true</c>.
   /// </summary>
   /// <remarks>
   /// Set to <c>false</c> for more performance if the same temp table is re-used very often.
   /// Set <see cref="TruncateTableIfExists"/> to <c>true</c> on re-use.
   /// </remarks>
   bool DropTableOnDispose { get; }

   /// <summary>
   /// Provides the name to create a temp table with.
   /// The default is the <see cref="ReusingTempTableNameProvider"/>.
   /// </summary>
   ITempTableNameProvider? TableNameProvider { get; }

   /// <summary>
   /// Provides the corresponding columns if the primary key should be created.
   /// The default is <see cref="IPrimaryKeyPropertiesProvider.EntityTypeConfiguration"/>.
   /// </summary>
   IPrimaryKeyPropertiesProvider? PrimaryKeyCreation { get; }

   /// <summary>
   /// Gets properties to insert.
   /// </summary>
   IEntityPropertiesProvider? PropertiesToInsert { get; }
}
