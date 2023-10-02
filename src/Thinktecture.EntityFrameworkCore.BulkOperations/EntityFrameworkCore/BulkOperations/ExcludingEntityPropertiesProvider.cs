using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal sealed class ExcludingEntityPropertiesProvider : IEntityPropertiesProvider
{
   private readonly IReadOnlyList<MemberInfo> _members;
   private readonly IReadOnlyList<IPropertyBase>? _properties;

   public ExcludingEntityPropertiesProvider(IReadOnlyList<MemberInfo> members)
   {
      _members = members ?? throw new ArgumentNullException(nameof(members));
   }

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
      if (_properties is not null)
         return _properties;

      var properties = new List<IPropertyBase>();

      for (var i = 0; i < _members.Count; i++)
      {
         var member = _members[i];

         var scalarProperty = entityType.FindProperty(member);

         if (scalarProperty is not null)
         {
            properties.Add(scalarProperty);
            continue;
         }

         var complexProperty = entityType.FindComplexProperty(member);

         if (complexProperty is not null)
         {
            properties.AddRange(complexProperty.ComplexType.GetFlattenedProperties());
            continue;
         }

         var navigation = entityType.FindNavigation(member);

         if (navigation is not null)
         {
            properties.Add(navigation);
            continue;
         }

         throw new NotSupportedException($"The entity '{entityType.ClrType.FullName}' either has no member with the name '{member.Name}' or the member is not supported by the '{nameof(IEntityPropertiesProvider)}'.");
      }

      return properties;
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
