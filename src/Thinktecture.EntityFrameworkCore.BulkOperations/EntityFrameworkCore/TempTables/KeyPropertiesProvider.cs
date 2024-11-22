using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

internal class KeyPropertiesProvider : IPrimaryKeyPropertiesProvider
{
   private readonly IReadOnlyList<MemberInfo> _members;

   public KeyPropertiesProvider(IReadOnlyList<MemberInfo> members)
   {
      _members = members;
   }

   public IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties)
   {
      var keyProperties = _members.ConvertToEntityProperties(entityType);
      var missingColumns = keyProperties.Except(tempTableProperties);

      if (missingColumns.Any())
      {
         throw new ArgumentException($"""
                                      Not all key columns are part of the table.
                                      Missing columns: {String.Join(", ", missingColumns.Select(p => p.GetColumnName()))}.
                                      """);
      }

      return keyProperties;
   }
}
