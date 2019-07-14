using System.Collections.Generic;
using System.Reflection;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Provides properties to work with.
   /// </summary>
   public interface IPropertiesProvider
   {
      /// <summary>
      /// Gets properties to work with.
      /// </summary>
      /// <returns>A collection <see cref="PropertyInfo"/>.</returns>
      IReadOnlyList<PropertyInfo> GetProperties();
   }
}
