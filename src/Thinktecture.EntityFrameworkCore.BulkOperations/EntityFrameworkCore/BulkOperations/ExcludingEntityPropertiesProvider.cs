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

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return Filter(IEntityPropertiesProvider.Default.GetPropertiesForTempTable(entityType, inlinedOwnTypes));
   }

   private IReadOnlyList<PropertyWithNavigations> Filter(IReadOnlyList<PropertyWithNavigations> properties)
   {
      return properties.Where(p => _members.All(m => m != p.Property.PropertyInfo && m != p.Property.FieldInfo))
                       .ToList();
   }

   public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return Filter(IEntityPropertiesProvider.Default.GetKeyProperties(entityType, inlinedOwnTypes));
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
