using System.Collections.Generic;
using System.Data;
using System.Reflection;
using JetBrains.Annotations;
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
      [NotNull]
      IReadOnlyList<IProperty> Properties { get; }

      /// <summary>
      /// Gets the index of the provided <paramref name="property"/> that matches with the one of <see cref="IDataRecord.GetValue"/>.
      /// </summary>
      /// <param name="property">Property info to get the index for.</param>
      /// <returns>Index of the property.</returns>
#pragma warning disable CA1716
      int GetPropertyIndex([NotNull] IProperty property);
#pragma warning restore CA1716
   }
}
