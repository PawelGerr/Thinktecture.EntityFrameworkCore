using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal sealed class IncludingEntityPropertiesProvider : IEntityPropertiesProvider
{
   private readonly IReadOnlyList<MemberInfo> _members;

   public IncludingEntityPropertiesProvider(IReadOnlyList<MemberInfo> members)
   {
      _members = members ?? throw new ArgumentNullException(nameof(members));
   }

   public IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType)
   {
      return _members.ConvertToEntityProperties(entityType, static _ => true);
   }

   public IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
   {
      return _members.ConvertToEntityProperties(entityType, static _ => true);
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return GetProperties(entityType, inlinedOwnTypes, IEntityPropertiesProvider.InsertAndUpdateFilter);
   }

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return GetProperties(entityType, inlinedOwnTypes, IEntityPropertiesProvider.InsertAndUpdateFilter);
   }

   private IReadOnlyList<PropertyWithNavigations> GetProperties(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      return _members.ConvertToEntityPropertiesWithNavigations(entityType, inlinedOwnTypes, filter);
   }
}
