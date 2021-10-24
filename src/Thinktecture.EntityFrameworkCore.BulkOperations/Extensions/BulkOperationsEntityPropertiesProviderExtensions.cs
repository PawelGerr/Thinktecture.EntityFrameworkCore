using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Data;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="IEntityPropertiesProvider"/>.
   /// </summary>
   public static class BulkOperationsEntityPropertiesProviderExtensions
   {
      /// <summary>
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
      /// <returns>Properties to include into a temp table.</returns>
      public static IReadOnlyList<PropertyWithNavigations> DeterminePropertiesForTempTable(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType,
         bool? inlinedOwnTypes)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<PropertyWithNavigations> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetPropertiesForTempTable(entityType, inlinedOwnTypes, TempTableFilter)).Count == 0
                   ? DetermineProperties(entityType, inlinedOwnTypes, TempTableFilter)
                   : properties;
      }

      /// <summary>
      /// Determines key properties.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
      /// <returns>Key properties.</returns>
      public static IReadOnlyList<PropertyWithNavigations> DetermineKeyProperties(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType,
         bool? inlinedOwnTypes)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<PropertyWithNavigations>? properties;

         if (entityPropertiesProvider is null || (properties = entityPropertiesProvider.GetKeyProperties(entityType, inlinedOwnTypes, KeyPropertyFilter)).Count == 0)
         {
            var pk = entityType.FindPrimaryKey()?.Properties;

            if (pk is null or { Count: 0 })
               throw new InvalidOperationException($"The entity '{entityType.Name}' has no primary key. Please provide key properties to perform JOIN/match on.");

            properties = pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
         }
         else
         {
            if (properties is null or { Count: 0 })
               throw new ArgumentException("The number of key properties to perform JOIN/match on cannot be 0.");
         }

         return properties;
      }

      /// <summary>
      /// Determines properties to insert into a (temp) table.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
      /// <returns>Properties to insert into a (temp) table.</returns>
      public static IReadOnlyList<PropertyWithNavigations> DeterminePropertiesForInsert(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType,
         bool? inlinedOwnTypes)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<PropertyWithNavigations> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetPropertiesForInsert(entityType, inlinedOwnTypes, InsertAndUpdateFilter)).Count == 0
                   ? DetermineProperties(entityType, inlinedOwnTypes, InsertAndUpdateFilter)
                   : properties;
      }

      /// <summary>
      /// Determines properties to use in update of a table.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
      /// <returns>Properties to use in update of a table.</returns>
      public static IReadOnlyList<PropertyWithNavigations> DeterminePropertiesForUpdate(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType,
         bool? inlinedOwnTypes)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<PropertyWithNavigations> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetPropertiesForUpdate(entityType, inlinedOwnTypes, InsertAndUpdateFilter)).Count == 0
                   ? DetermineProperties(entityType, inlinedOwnTypes, InsertAndUpdateFilter)
                   : properties;
      }

      private static IReadOnlyList<PropertyWithNavigations> DetermineProperties(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         var properties = entityType.GetProperties()
                                    .Where(p => filter(p, Array.Empty<INavigation>()))
                                    .Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>()))
                                    .ToList();

         foreach (var navigation in entityType.GetOwnedTypesProperties(inlinedOwnTypes))
         {
            var navigations = new[] { navigation };
            properties.AddPropertiesAndOwnedTypesRecursively(navigation.TargetEntityType, navigations, inlinedOwnTypes, filter);
         }

         return properties;
      }

      internal static void AddPropertiesAndOwnedTypesRecursively(
         this List<PropertyWithNavigations> properties,
         IEntityType entityType,
         IReadOnlyList<INavigation> navigations,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         foreach (var property in entityType.GetProperties().Where(p => filter(p, navigations)))
         {
            properties.Add(new PropertyWithNavigations(property, navigations));
         }

         foreach (var ownedTypeNavigation in entityType.GetOwnedTypesProperties(inlinedOwnTypes))
         {
            var innerNavigations = navigations.ToList();
            innerNavigations.Add(ownedTypeNavigation);

            properties.AddPropertiesAndOwnedTypesRecursively(ownedTypeNavigation.TargetEntityType, innerNavigations, inlinedOwnTypes, filter);
         }
      }

      private static bool TempTableFilter(IProperty property, IReadOnlyList<INavigation> navigations)
      {
         return navigations.Count == 0 || !property.IsKey();
      }

      private static bool KeyPropertyFilter(IProperty property, IReadOnlyList<INavigation> navigations)
      {
         return true;
      }

      private static bool InsertAndUpdateFilter(IProperty property, IReadOnlyList<INavigation> navigations)
      {
         return property.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore &&
                (navigations.Count == 0 || !navigations[^1].IsInlined() || !property.IsKey());
      }
   }
}
