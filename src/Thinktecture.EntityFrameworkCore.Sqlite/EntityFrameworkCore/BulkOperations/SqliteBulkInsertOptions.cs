using System;
using System.Collections.Generic;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by <see cref="SqliteDbContextExtensions.BulkInsertAsync{T}"/>.
   /// </summary>
   public class SqliteBulkInsertOptions : IBulkInsertOptions
   {
      /// <inheritdoc />
      public IEntityMembersProvider EntityMembersProvider { get; set; }
   }
}
