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
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         var properties = new List<PropertyWithNavigations>();
         var navigations = Array.Empty<INavigation>();

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

               properties.AddPropertiesAndOwnedTypesRecursively(ownedNavi.TargetEntityType, new[] { ownedNavi }, inlinedOwnTypes, filter);
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
               if (ownedTypeNavi.IsInlined())
                  return ownedTypeNavi;

               throw new NotSupportedException($"Properties of owned types that are saved in a separate table are not supported. Property: {ownedTypeNavi.Name}");
            }
         }

         return null;
      }

      /// <summary>
      /// Separates <paramref name="properties"/> in own properties and properties belonging to different tables.
      /// </summary>
      /// <param name="properties">Properties to separate.</param>
      /// <returns>A collection of own and "external" properties.</returns>
      public static (IReadOnlyList<PropertyWithNavigations> sameTable, IReadOnlyList<PropertyWithNavigations> otherTable) SeparateProperties(
         this IReadOnlyList<PropertyWithNavigations> properties)
      {
         var sameTableProperties = new List<PropertyWithNavigations>();
         List<PropertyWithNavigations>? externalProperties = null;

         foreach (var property in properties)
         {
            if (property.IsInlined)
            {
               sameTableProperties.Add(property);
            }
            else
            {
               externalProperties ??= new List<PropertyWithNavigations>();
               externalProperties.Add(property);
            }
         }

         return (sameTableProperties, (IReadOnlyList<PropertyWithNavigations>?)externalProperties ?? Array.Empty<PropertyWithNavigations>());
      }

      /// <summary>
      /// Groups <paramref name="externalProperties"/> by the owned type property they belong to.
      /// </summary>
      /// <param name="externalProperties">Properties to group.</param>
      /// <param name="parentEntities">A collection of the parent properties.</param>
      /// <returns>A collection of tuples of the owned type navigation, owned type entities and their properties.</returns>
      public static IEnumerable<(INavigation, IEnumerable<object>, IReadOnlyList<PropertyWithNavigations>)> GroupExternalProperties(
         this IReadOnlyList<PropertyWithNavigations> externalProperties,
         IReadOnlyList<object> parentEntities)
      {
         foreach (var group in externalProperties.Select(p => p.DropUntilSeparateNavigation())
                                                 .GroupBy(t => t, DroppedInlineNavigationsComparer.Instance))
         {
            var navigation = group.Key.DroppedNavigations.Last();
            var ownedEntities = group.Key.DroppedNavigations.Aggregate((IEnumerable<object>)parentEntities, ProjectEntities);
            var properties = group.Select(g => g.Property).ToList();

            yield return (navigation, ownedEntities, properties);
         }
      }

      private static IEnumerable<object> ProjectEntities(IEnumerable<object> ownedEntities, INavigation navigation)
      {
         var getter = navigation.GetGetter() ?? throw new Exception($"No property-getter for the navigational property '{navigation.ClrType.Name}.{navigation.PropertyInfo.Name}' found.");

         ownedEntities = ownedEntities.Select(getter.GetClrValue);

         if (navigation.IsCollection)
            ownedEntities = ownedEntities.SelectMany(c => (IEnumerable<object>)c);

         return ownedEntities;
      }
   }

   /// <summary>
   /// Uses "DroppedNavigations" only.
   /// </summary>
   internal class DroppedInlineNavigationsComparer
      : IEqualityComparer<(IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property)>
   {
      public static DroppedInlineNavigationsComparer Instance = new();

      public bool Equals(
         (IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) x,
         (IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) y)
      {
         if (x.DroppedNavigations.Count != y.DroppedNavigations.Count)
            return false;

         for (var i = 0; i < x.DroppedNavigations.Count; i++)
         {
            if (!x.DroppedNavigations[i].Equals(y.DroppedNavigations[i]))
               return false;
         }

         return true;
      }

      public int GetHashCode((IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) obj)
      {
         var hashCode = new HashCode();

         foreach (var navigation in obj.DroppedNavigations)
         {
            hashCode.Add(navigation);
         }

         return hashCode.ToHashCode();
      }
   }
}
