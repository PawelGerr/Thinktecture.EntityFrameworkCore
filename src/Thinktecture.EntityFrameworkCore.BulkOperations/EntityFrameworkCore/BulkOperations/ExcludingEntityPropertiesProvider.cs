using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal sealed class ExcludingEntityPropertiesProvider : IEntityPropertiesProvider
{
   private readonly IReadOnlyList<MemberInfo> _members;

   public ExcludingEntityPropertiesProvider(IReadOnlyList<MemberInfo> members)
   {
      _members = members ?? throw new ArgumentNullException(nameof(members));
   }

   public IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType)
   {
      return Filter(IEntityPropertiesProvider.Default.GetPropertiesForTempTable(entityType));
   }

   private IReadOnlyList<PropertyWithNavigations> Filter(IReadOnlyList<PropertyWithNavigations> properties)
   {
      return properties.Where(p => _members.All(m => m != p.Property.PropertyInfo && m != p.Property.FieldInfo))
                       .ToList();
   }

   private IReadOnlyList<IProperty> Filter(IReadOnlyList<IProperty> properties)
   {
      return properties.Where(p => _members.All(m => m != p.PropertyInfo && m != p.FieldInfo))
                       .ToList();
   }

   public IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
   {
      return Filter(IEntityPropertiesProvider.Default.GetKeyProperties(entityType));
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return Filter(IEntityPropertiesProvider.Default.GetPropertiesForInsert(entityType, inlinedOwnTypes));
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return Filter(IEntityPropertiesProvider.Default.GetPropertiesForUpdate(entityType, inlinedOwnTypes));
   }
}
