using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class DefaultPropertiesEntityPropertiesProvider : IEntityPropertiesProvider
{
   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return DetermineProperties(entityType, inlinedOwnTypes, IEntityPropertiesProvider.TempTableFilter);
   }

   public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(IEntityType entityType, bool? inlinedOwnTypes)
   {
      var pk = entityType.FindPrimaryKey()?.Properties;

      if (pk is null or { Count: 0 })
         throw new InvalidOperationException($"The entity '{entityType.Name}' has no primary key. Please provide key properties to perform JOIN/match on.");

      return pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return DetermineProperties(entityType, inlinedOwnTypes, IEntityPropertiesProvider.InsertAndUpdateFilter);
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return DetermineProperties(entityType, inlinedOwnTypes, IEntityPropertiesProvider.InsertAndUpdateFilter);
   }

   private static IReadOnlyList<PropertyWithNavigations> DetermineProperties(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      var properties = entityType.GetProperties()
                                 .Where(p => filter(p, Array.Empty<INavigation>()))
                                 .Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>()))
                                 .ToList();

      foreach (var navigation in entityType.GetOwnedTypesProperties(inlinedOwnTypes))
      {
         var navigations = new[] { navigation };
         properties.AddPropertiesAndOwnedTypesRecursively(navigation.TargetEntityType, navigations, inlinedOwnTypes, filter);
      }

      return properties;
   }
}
