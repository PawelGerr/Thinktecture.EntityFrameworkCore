using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public interface ITempTableCreationOptions
   {
      /// <summary>
      /// Indication whether the table name should be unique.
      /// Default is <c>true</c>.
      /// </summary>
      bool MakeTableNameUnique { get; }

      /// <summary>
      /// Indication whether to create the primary key along with the creation of the temp table.
      /// Default is <c>true</c>.
      /// </summary>
      bool CreatePrimaryKey { get; }

      /// <summary>
      /// Properties to create temp table with.
      /// If the <see cref="EntityMembersProvider"/> is <c>null</c> then the tamp table will contain all properties of the entity.
      /// </summary>
      IEntityMembersProvider? EntityMembersProvider { get; }
   }
}
