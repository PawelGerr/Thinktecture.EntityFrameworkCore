using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal sealed class AdaptiveEntityTypeConfigurationPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
   {
      if (tempTableProperties.Count == 0)
         return Array.Empty<PropertyWithNavigations>();

      var pk = entityType.FindPrimaryKey()?.Properties;

      if (pk is null or { Count: 0 })
         return Array.Empty<PropertyWithNavigations>();

      return pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>()))
               .Intersect(tempTableProperties)
               .ToList();
   }
}
