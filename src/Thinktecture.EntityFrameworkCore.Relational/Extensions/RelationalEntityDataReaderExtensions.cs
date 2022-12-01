using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public static class RelationalEntityDataReaderExtensions
{
   public static int GetPropertyIndex(this IEntityDataReader reader, IProperty property)
   {
      return reader.GetPropertyIndex(new PropertyWithNavigations(property, Array.Empty<Navigation>()));
   }
}
