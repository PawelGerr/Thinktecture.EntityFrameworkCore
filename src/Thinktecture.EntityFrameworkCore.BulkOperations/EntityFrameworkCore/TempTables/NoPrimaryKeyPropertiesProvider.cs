using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal sealed class NoPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   public IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties)
   {
      ArgumentNullException.ThrowIfNull(entityType);
      ArgumentNullException.ThrowIfNull(tempTableProperties);

      return Array.Empty<IProperty>();
   }
}
