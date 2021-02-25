using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options for bulk insert into a temp table.
   /// </summary>
   public interface ISqlServerTempTableBulkInsertOptions : ITempTableBulkInsertOptions
   {
      /// <summary>
      /// Provides the corresponding columns if the primary key should be created.
      /// The default is <see cref="PrimaryKeyPropertiesProviders.EntityTypeConfiguration"/>.
      /// </summary>
      IPrimaryKeyPropertiesProvider PrimaryKeyCreation { get; }

      /// <summary>
      /// Defines when the primary key should be created.
      /// Default is set to <see cref="MomentOfSqlServerPrimaryKeyCreation.AfterBulkInsert"/>.
      /// </summary>
      MomentOfSqlServerPrimaryKeyCreation MomentOfPrimaryKeyCreation { get; }

      /// <summary>
      /// Options for creation of the temp table.
      /// </summary>
      new ISqlServerTempTableCreationOptions TempTableCreationOptions { get; }

      /// <summary>
      /// Gets properties for creation of the temp table and for insert.
      /// </summary>
      IEntityPropertiesProvider? PropertiesToInsert { get; }
   }
}
