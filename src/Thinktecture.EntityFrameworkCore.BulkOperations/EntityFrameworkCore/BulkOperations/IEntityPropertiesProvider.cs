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
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to include into a temp table.</returns>
      IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType);

      /// <summary>
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to include into a temp table.</returns>
      IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType);

      /// <summary>
      /// Determines properties to insert into a (temp) table.
      /// </summary>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to insert into a (temp) table.</returns>
      IReadOnlyList<IProperty> GetPropertiesForInsert(IEntityType entityType);

      /// <summary>
      /// Determines properties to use in update of a table.
      /// </summary>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to use in update of a table.</returns>
      IReadOnlyList<IProperty> GetPropertiesForUpdate(IEntityType entityType);
   }
}
