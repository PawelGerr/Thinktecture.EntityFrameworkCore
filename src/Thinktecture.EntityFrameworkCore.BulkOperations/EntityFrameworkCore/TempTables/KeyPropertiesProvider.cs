using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal class KeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   private readonly IReadOnlyList<MemberInfo> _members;

   public KeyPropertiesProvider(IReadOnlyList<MemberInfo> members)
   {
      _members = members;
   }

   public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
   {
      var keyProperties = _members.ConvertToEntityProperties(entityType, true, NoFilter);
      var missingColumns = keyProperties.Except(tempTableProperties);

      if (missingColumns.Any())
      {
         throw new ArgumentException(@$"Not all key columns are part of the table.
Missing columns: {String.Join(", ", missingColumns.Select(p => p.Property.GetColumnName()))}.");
      }

      return keyProperties;
   }

   private static bool NoFilter(IProperty property, IReadOnlyList<INavigation> navigations)
   {
      return true;
   }
}
