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

   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return GetProperties(entityType, inlinedOwnTypes, IEntityPropertiesProvider.TempTableFilter);
   }

   public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(IEntityType entityType, bool? inlinedOwnTypes)
   {
      return GetProperties(entityType, inlinedOwnTypes, static (_, _) => true);
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
      return _members.ConvertToEntityProperties(entityType, inlinedOwnTypes, filter);
   }
}
