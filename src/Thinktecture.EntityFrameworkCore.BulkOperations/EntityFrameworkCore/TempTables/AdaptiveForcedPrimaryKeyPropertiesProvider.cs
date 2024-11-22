using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal sealed class AdaptiveForcedPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   public IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties)
   {
      if (tempTableProperties.Count == 0)
         return Array.Empty<IProperty>();

      var pk = entityType.FindPrimaryKey()?.Properties;

      if (pk is null or { Count: 0 })
         return tempTableProperties;

      return pk.Intersect(tempTableProperties).ToList();
   }
}
