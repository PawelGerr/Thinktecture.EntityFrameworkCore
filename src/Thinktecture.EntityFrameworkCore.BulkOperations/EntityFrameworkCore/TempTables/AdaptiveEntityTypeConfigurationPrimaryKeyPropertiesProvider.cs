using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal sealed class AdaptiveEntityTypeConfigurationPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   public IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties)
   {
      if (tempTableProperties.Count == 0)
         return Array.Empty<IProperty>();

      var pk = entityType.FindPrimaryKey()?.Properties;

      if (pk is null or { Count: 0 })
         return Array.Empty<IProperty>();

      return pk.Intersect(tempTableProperties).ToList();
   }
}
