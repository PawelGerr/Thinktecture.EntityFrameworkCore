using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal sealed class ConfiguredPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
   {
      ArgumentNullException.ThrowIfNull(entityType);
      ArgumentNullException.ThrowIfNull(tempTableProperties);

      var pk = entityType.FindPrimaryKey()?.Properties;

      if (pk is null or { Count: 0 })
         return Array.Empty<PropertyWithNavigations>();

      var keyProperties = pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      var missingColumns = keyProperties.Except(tempTableProperties);

      if (missingColumns.Any())
      {
         throw new ArgumentException(@$"Cannot create PRIMARY KEY because not all key columns are part of the temp table.
You may use other key properties providers like '{nameof(PrimaryKeyPropertiesProviders)}.{nameof(IPrimaryKeyPropertiesProvider.AdaptiveEntityTypeConfiguration)}' instead of '{nameof(PrimaryKeyPropertiesProviders)}.{nameof(IPrimaryKeyPropertiesProvider.EntityTypeConfiguration)}' to get different behaviors.
Missing columns: {String.Join(", ", missingColumns.Select(p => p.Property.GetColumnBaseName()))}.");
      }

      return keyProperties;
   }
}
