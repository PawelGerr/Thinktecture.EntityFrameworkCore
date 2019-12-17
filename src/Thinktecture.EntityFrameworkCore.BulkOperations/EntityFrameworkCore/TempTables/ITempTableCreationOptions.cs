using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public interface ITempTableCreationOptions
   {
      /// <summary>
      /// Truncates/drops the temp table before "creation" if the table exists already.
      /// Default is <c>false</c>.
      /// </summary>
      /// <remarks>
      /// If the database supports "truncate" then the table is going to be truncated otherwise the table is dropped.
      /// If the property is set to <c>false</c> then the temp table is considered a "new table", i.e. no "EXISTS" checks are made.
      /// </remarks>
      public bool TruncateTableIfExists { get; set; }

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

      /// <summary>
      /// Indication whether to drop the temp table on dispose of <see cref="ITempTableQuery{T}"/>.
      /// Default is <c>true</c>.
      /// </summary>
      /// <remarks>
      /// Set to <c>false</c> for more performance if the same temp table is re-used very often.
      /// Set <see cref="TruncateTableIfExists"/> to <c>true</c> on re-use.
      /// </remarks>
      bool DropTableOnDispose { get; }
   }
}
