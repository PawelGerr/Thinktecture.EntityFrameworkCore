using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for collections.
   /// </summary>
   public static class BulkOperationsCollectionExtensions
   {
      internal static IReadOnlyList<PropertyWithNavigations> ConvertToEntityProperties(
         this IReadOnlyList<MemberInfo> members,
         IEntityType entityType,
         IReadOnlyList<INavigation> navigations,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         var properties = new List<PropertyWithNavigations>();

         foreach (var memberInfo in members)
         {
            var property = FindProperty(entityType, memberInfo);

            if (property != null)
            {
               if (!filter(property, navigations))
                  throw new InvalidOperationException($"The member '{memberInfo.Name}' of the entity '{entityType.Name}' cannot be written to database.");

               properties.Add(new PropertyWithNavigations(property, navigations));
            }
            else
            {
               var ownedNavi = FindOwnedProperty(entityType, memberInfo);

               if (ownedNavi == null)
                  throw new Exception($"The member '{memberInfo.Name}' has not been found on entity '{entityType.Name}'.");

               var innerNavigations = navigations.ToList();
               innerNavigations.Add(ownedNavi);

               properties.AddPropertiesAndOwnedTypesRecursively(ownedNavi.TargetEntityType, innerNavigations, inlinedOwnTypes, filter);
            }
         }

         return properties;
      }

      private static IProperty? FindProperty(IEntityType entityType, MemberInfo memberInfo)
      {
         return entityType.GetProperties().FirstOrDefault(property => property.PropertyInfo == memberInfo || property.FieldInfo == memberInfo);
      }

      private static INavigation? FindOwnedProperty(
         IEntityType entityType,
         MemberInfo memberInfo)
      {
         foreach (var ownedTypeNavi in entityType.GetOwnedTypesProperties(null)) // search for all owned properties, i.e., don't use "inlinedOwnTypes" from the caller
         {
            if (ownedTypeNavi.PropertyInfo == memberInfo || ownedTypeNavi.FieldInfo == memberInfo)
            {
               if (ownedTypeNavi.IsOwnedTypeInline())
                  return ownedTypeNavi;

               throw new NotSupportedException($"Properties of owned types that are saved in a separate table are not supported. Property: {ownedTypeNavi.Name}");
            }
         }

         return null;
      }
   }
}
