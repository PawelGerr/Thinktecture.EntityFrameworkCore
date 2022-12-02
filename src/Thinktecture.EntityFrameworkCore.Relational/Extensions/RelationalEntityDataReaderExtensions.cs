using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="IEntityDataReader"/>.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public static class RelationalEntityDataReaderExtensions
{
   /// <summary>
   /// Gets the index of the provided <paramref name="property"/>.
   /// </summary>
   /// <param name="reader">Entity data reader.</param>
   /// <param name="property">Property to get the index for.</param>
   /// <returns>Index of the property.</returns>
   public static int GetPropertyIndex(this IEntityDataReader reader, IProperty property)
   {
      return reader.GetPropertyIndex(new PropertyWithNavigations(property, Array.Empty<Navigation>()));
   }
}
