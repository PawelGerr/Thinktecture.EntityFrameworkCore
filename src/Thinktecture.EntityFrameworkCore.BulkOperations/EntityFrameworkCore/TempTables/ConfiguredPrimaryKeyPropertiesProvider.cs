using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal sealed class ConfiguredPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   public IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties)
   {
      ArgumentNullException.ThrowIfNull(entityType);
      ArgumentNullException.ThrowIfNull(tempTableProperties);

      var pk = entityType.FindPrimaryKey()?.Properties;

      if (pk is null or { Count: 0 })
         return Array.Empty<IProperty>();

      var missingColumns = pk.Except(tempTableProperties);

      if (missingColumns.Any())
      {
         throw new ArgumentException($"""
                                      Cannot create PRIMARY KEY because not all key columns are part of the temp table.
                                      You may use other key properties providers like '{nameof(IPrimaryKeyPropertiesProvider)}.{nameof(IPrimaryKeyPropertiesProvider.AdaptiveEntityTypeConfiguration)}' instead of '{nameof(IPrimaryKeyPropertiesProvider)}.{nameof(IPrimaryKeyPropertiesProvider.EntityTypeConfiguration)}' to get different behaviors.
                                      Missing columns: {String.Join(", ", missingColumns.Select(p => p.GetColumnName()))}.
                                      """);
      }

      return pk;
   }
}
