using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Data;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

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
   /// <returns>Properties to include into a temp table.</returns>
   public static IReadOnlyList<IProperty> DeterminePropertiesForTempTable(
      this IEntityPropertiesProvider? entityPropertiesProvider,
      IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      return (entityPropertiesProvider ?? IEntityPropertiesProvider.Default).GetPropertiesForTempTable(entityType);
   }

   /// <summary>
   /// Determines key properties.
   /// </summary>
   /// <param name="entityPropertiesProvider">Entity properties provider.</param>
   /// <param name="entityType">Entity type.</param>
   /// <returns>Key properties.</returns>
   public static IReadOnlyList<IProperty> DetermineKeyProperties(
      this IEntityPropertiesProvider? entityPropertiesProvider,
      IEntityType entityType)
   {
      ArgumentNullException.ThrowIfNull(entityType);

      var properties = (entityPropertiesProvider ?? IEntityPropertiesProvider.Default).GetKeyProperties(entityType);

      if (properties is null or { Count: 0 })
         throw new ArgumentException("The number of key properties to perform JOIN/match on cannot be 0.");

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
      ArgumentNullException.ThrowIfNull(entityType);

      return (entityPropertiesProvider ?? IEntityPropertiesProvider.Default).GetPropertiesForInsert(entityType, inlinedOwnTypes);
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
      ArgumentNullException.ThrowIfNull(entityType);

      return (entityPropertiesProvider ?? IEntityPropertiesProvider.Default).GetPropertiesForUpdate(entityType, inlinedOwnTypes);
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
}
