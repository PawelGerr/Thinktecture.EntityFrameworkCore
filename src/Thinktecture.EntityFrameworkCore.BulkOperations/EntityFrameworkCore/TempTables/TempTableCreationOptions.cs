using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Options required for creation of a temp table
   /// </summary>
   public sealed class TempTableCreationOptions : ITempTableCreationOptions
   {
      /// <inheritdoc />
      public bool MakeTableNameUnique { get; set; } = true;

      /// <inheritdoc />
      public bool CreatePrimaryKey { get; set; } = true;

      /// <inheritdoc />
      public IEntityMembersProvider? MembersToInclude { get; set; }
   }
}
