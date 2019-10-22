using System;
using System.Collections.Generic;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Bulk insert options.
   /// </summary>
   public interface IBulkInsertOptions
   {
      /// <summary>
      /// Properties to insert.
      /// If the <see cref="EntityMembersProvider"/> is null then all properties of the entity are going to be inserted.
      /// </summary>
      IEntityMembersProvider? EntityMembersProvider { get; set; }
   }
}
