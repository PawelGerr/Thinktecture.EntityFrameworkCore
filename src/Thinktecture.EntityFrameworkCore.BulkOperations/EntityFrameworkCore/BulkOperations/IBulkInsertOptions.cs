using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Options used by <see cref="DbContextExtensions.BulkInsertAsync{T}(Microsoft.EntityFrameworkCore.DbContext,System.Collections.Generic.IEnumerable{T},IBulkInsertOptions,System.Threading.CancellationToken)"/> and similar method overloads..
   /// </summary>
   public interface IBulkInsertOptions
   {
      /// <summary>
      /// Properties to insert.
      /// If the <see cref="EntityMembersProvider"/> is null then all properties of the entity are going to be inserted.
      /// </summary>
      [CanBeNull]
      IEntityMembersProvider EntityMembersProvider { get; set; }
   }
}
