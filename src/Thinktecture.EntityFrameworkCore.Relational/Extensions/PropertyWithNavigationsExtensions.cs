using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="PropertyWithNavigations"/>.
/// </summary>
public static class PropertyWithNavigationsExtensions
{
   /// <summary>
   /// Gets the <see cref="StoreObjectIdentifier"/> for <paramref name="property"/>.
   /// </summary>
   /// <param name="property">Property to get <see cref="StoreObjectIdentifier"/> for.</param>
   /// <returns>The <see cref="StoreObjectIdentifier"/>.</returns>
   /// <exception cref="Exception">If no <see cref="StoreObjectIdentifier"/> found.</exception>
   public static StoreObjectIdentifier GetStoreObject(this PropertyWithNavigations property)
   {
      return StoreObjectIdentifier.Create(property.Property.DeclaringEntityType, StoreObjectType.Table)
             ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.Property.DeclaringEntityType.Name}'.");
   }

   /// <summary>
   /// Gets the <see cref="StoreObjectIdentifier"/> for <paramref name="property"/>.
   /// </summary>
   /// <param name="property">Property to get <see cref="StoreObjectIdentifier"/> for.</param>
   /// <returns>The <see cref="StoreObjectIdentifier"/>.</returns>
   /// <exception cref="Exception">If no <see cref="StoreObjectIdentifier"/> found.</exception>
   public static StoreObjectIdentifier GetStoreObject(this IProperty property)
   {
      return StoreObjectIdentifier.Create(property.DeclaringEntityType, StoreObjectType.Table)
             ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{property.DeclaringEntityType.Name}'.");
   }

   /// <summary>
   /// Gets the column name for <paramref name="property"/>.
   /// </summary>
   /// <param name="property">Property to get column name for.</param>
   /// <param name="storeObject">Store object.</param>
   /// <returns>Column name</returns>
   /// <exception cref="Exception">If no column name found.</exception>
   public static string GetColumnName(this PropertyWithNavigations property, StoreObjectIdentifier storeObject)
   {
      return property.Property.GetColumnName(storeObject)
             ?? throw new Exception($"The property '{property.Property.Name}' has no column name.");
   }
}
