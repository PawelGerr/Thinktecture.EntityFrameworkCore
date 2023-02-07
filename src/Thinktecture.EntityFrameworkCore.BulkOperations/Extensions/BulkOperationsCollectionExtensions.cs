using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for collections.
/// </summary>
public static class BulkOperationsCollectionExtensions
{
   internal static IReadOnlyList<IProperty> ConvertToEntityProperties(
      this IReadOnlyList<MemberInfo> members,
      IEntityType entityType,
      Func<IProperty, bool> filter)
   {
      if (entityType.GetOwnedTypesProperties(null).Any())
         throw new NotSupportedException($"The entity '{entityType.Name}' must not contain owned entities.");

      var properties = new List<IProperty>();

      foreach (var memberInfo in members)
      {
         var property = FindProperty(entityType, memberInfo);

         if (property != null && filter(property))
         {
            properties.Add(property);
            continue;
         }

         throw new InvalidOperationException($"The member '{memberInfo.Name}' of the entity '{entityType.Name}' cannot be written to database.");
      }

      return properties;
   }

   internal static IReadOnlyList<PropertyWithNavigations> ConvertToEntityPropertiesWithNavigations(
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
      return entityType.GetProperties().FirstOrDefault(property => property.PropertyInfo?.MetadataToken == memberInfo.MetadataToken
                                                                   || property.FieldInfo?.MetadataToken == memberInfo.MetadataToken);
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
   /// Checks that there are no separate owned types inside a collection property.
   /// </summary>
   /// <param name="properties">Properties to check.</param>
   /// <exception cref="NotSupportedException">If a separate owned type is found inside a collection property.</exception>
   public static void EnsureNoSeparateOwnedTypesInsideCollectionOwnedType(this IReadOnlyList<PropertyWithNavigations> properties)
   {
      ArgumentNullException.ThrowIfNull(properties);

      foreach (var property in properties)
      {
         INavigation? collection = null;

         foreach (var navigation in property.Navigations)
         {
            if (!navigation.IsInlined() && collection is not null)
               throw new NotSupportedException($"Non-inlined (i.e. with its own table) nested owned type '{navigation.DeclaringEntityType.Name}.{navigation.Name}' inside another owned type collection '{collection.DeclaringEntityType.Name}.{collection.Name}' is not supported.");

            if (navigation.IsCollection)
               collection = navigation;
         }
      }
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
      var getter = navigation.GetGetter() ?? throw new Exception($"No property-getter for the navigational property '{navigation.ClrType.Name}.{navigation.PropertyInfo?.Name ?? navigation.Name}' found.");

      ownedEntities = ownedEntities.Select(getter.GetClrValue).Where(e => e != null)!;

      if (navigation.IsCollection)
         ownedEntities = ownedEntities.SelectMany(c => (IEnumerable<object>)c);

      return ownedEntities;
   }
}
