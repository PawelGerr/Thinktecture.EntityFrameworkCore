using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for collections.
   /// </summary>
   public static class BulkOperationsCollectionExtensions
   {
      internal static IReadOnlyList<IProperty> ConvertToEntityProperties(this IReadOnlyList<MemberInfo> memberInfos, IEntityType entityType)
      {
         if (memberInfos == null)
            throw new ArgumentNullException(nameof(memberInfos));
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         var properties = new IProperty[memberInfos.Count];

         for (var i = 0; i < memberInfos.Count; i++)
         {
            var memberInfo = memberInfos[i];
            properties[i] = FindProperty(entityType, memberInfo) ?? throw new ArgumentException($"The member '{memberInfo.Name}' has not found on entity '{entityType.Name}'.", nameof(memberInfos));
         }

         return properties;
      }

      private static IProperty? FindProperty(IEntityType entityType, MemberInfo memberInfo)
      {
         return entityType.GetProperties().FirstOrDefault(property => property.PropertyInfo == memberInfo || property.FieldInfo == memberInfo);
      }
   }
}
