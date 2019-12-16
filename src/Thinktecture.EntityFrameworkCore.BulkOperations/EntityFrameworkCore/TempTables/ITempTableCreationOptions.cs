using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public interface ITempTableCreationOptions
   {
      /// <summary>
      /// Drops/truncates the temp table if the table exists already.
      /// Default is <c>false</c>.
      /// </summary>
      bool DropTempTableIfExists { get; }

      /// <summary>
      /// Provides the name to create a temp table with.
      /// </summary>
      ITempTableNameProvider TableNameProvider { get; }

      /// <summary>
      /// Indication whether to create the primary key along with the creation of the temp table.
      /// Default is <c>true</c>.
      /// </summary>
      bool CreatePrimaryKey { get; }

      /// <summary>
      /// Properties to create temp table with.
      /// If the <see cref="MembersToInclude"/> is <c>null</c> then the tamp table will contain all properties of the entity.
      /// </summary>
      IEntityMembersProvider? MembersToInclude { get; }
   }
}
