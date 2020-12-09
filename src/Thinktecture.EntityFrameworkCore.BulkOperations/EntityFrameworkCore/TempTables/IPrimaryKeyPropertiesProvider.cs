using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Provides the properties to be used for creation of the primary key.
   /// </summary>
   public interface IPrimaryKeyPropertiesProvider
   {
      /// <summary>
      /// Gets the primary key properties.
      /// </summary>
      /// <param name="entityType">Entity type to get the primary key properties for.</param>
      /// <param name="tempTableProperties">Actual properties of the temp table.</param>
      /// <returns>
      /// Properties to use for creation of the primary key.
      /// </returns>
      IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties);
   }
}
