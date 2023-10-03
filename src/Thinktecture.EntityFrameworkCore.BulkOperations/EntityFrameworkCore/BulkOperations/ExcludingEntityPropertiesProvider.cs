using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal sealed class ExcludingEntityPropertiesProvider(
      IReadOnlyList<MemberInfo> members)
   : IEntityPropertiesProvider
{
   private (IEntityType Type, IReadOnlyList<IPropertyBase> Properties)? _cache;

   public IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType)
   {
      return Filter(entityType, IEntityPropertiesProvider.Default.GetPropertiesForTempTable(entityType));
   }

   private IReadOnlyList<PropertyWithNavigations> Filter(IEntityType entityType, IReadOnlyList<PropertyWithNavigations> properties)
   {
      var propertiesToExclude = GetPropertiesToExclude(entityType);
      return properties.Where(p => !propertiesToExclude.Contains(p.Property))
                       .ToList();
   }

   private IReadOnlyList<IProperty> Filter(IEntityType entityType, IReadOnlyList<IProperty> properties)
   {
      var propertiesToExclude = GetPropertiesToExclude(entityType);
      return properties.Where(p => !propertiesToExclude.Contains(p))
                       .ToList();
   }

   private IReadOnlyList<IPropertyBase> GetPropertiesToExclude(IEntityType entityType)
   {
      var cache = _cache;

      if (cache?.Type != entityType)
         _cache = cache = (entityType, members.ConvertToEntityProperties(entityType));

      return cache.Value.Properties;
   }

   public IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
   {
      return Filter(entityType, IEntityPropertiesProvider.Default.GetKeyProperties(entityType));
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return Filter(entityType, IEntityPropertiesProvider.Default.GetPropertiesForInsert(entityType, inlinedOwnTypes));
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return Filter(entityType, IEntityPropertiesProvider.Default.GetPropertiesForUpdate(entityType, inlinedOwnTypes));
   }
}
