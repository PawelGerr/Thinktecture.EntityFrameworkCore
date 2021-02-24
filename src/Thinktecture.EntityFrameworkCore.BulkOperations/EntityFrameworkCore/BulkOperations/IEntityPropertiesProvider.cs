using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Provides entity properties to work with.
   /// </summary>
   public interface IEntityPropertiesProvider
   {
      /// <summary>
      /// Gets properties to work with.
      /// </summary>
      /// <param name="entityType">The type of the entity to get the properties for.</param>
      /// <returns>A collection of <see cref="IProperty"/>.</returns>
      IReadOnlyList<IProperty> GetProperties(IEntityType entityType);
   }
}
