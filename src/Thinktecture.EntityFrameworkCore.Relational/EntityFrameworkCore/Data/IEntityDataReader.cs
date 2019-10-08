using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Data reader to be used for bulk inserts.
   /// </summary>
   public interface IEntityDataReader : IDataReader
   {
      /// <summary>
      /// Gets the properties the reader is created for.
      /// </summary>
      /// <returns>A collection of <see cref="PropertyInfo"/>.</returns>
      IReadOnlyList<IProperty> Properties { get; }

      /// <summary>
      /// Gets the index of the provided <paramref name="entityProperty"/> that matches with the one of <see cref="IDataRecord.GetValue"/>.
      /// </summary>
      /// <param name="entityProperty">Property to get the index for.</param>
      /// <returns>Index of the property.</returns>
      int GetPropertyIndex(IProperty entityProperty);
   }
}
