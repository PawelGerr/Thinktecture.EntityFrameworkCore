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

      /// <summary>
      /// Initializes current options using the provided <paramref name="options"/>.
      /// </summary>
      /// <param name="options">
      /// Options to be used for initialization of current instance.
      /// The <paramref name="options"/> may be of different type than the current instance.
      /// </param>
      /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
      /// <exception cref="NotSupportedException">Current instance cannot be initialized using the provided <paramref name="options"/>.</exception>
      void InitializeFrom(IBulkInsertOptions options);
   }
}
