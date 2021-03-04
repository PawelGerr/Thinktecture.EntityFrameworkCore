using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="IEntityType"/>.
   /// </summary>
   public static class BulkOperationsEntityTypeExtensions
   {
      /// <summary>
      /// Fetches owned types of the <paramref name="entityType"/>.
      /// </summary>
      /// <param name="entityType">Entity type to fetch owned types of.</param>
      /// <param name="inlinedOwnTypes">Indication whether to fetch inlined owned types, non-inlined or all of them.</param>
      /// <returns>Navigations pointing to owned types.</returns>
      /// <exception cref="ArgumentNullException"></exception>
      public static IEnumerable<INavigation> GetOwnedTypesProperties(
         this IEntityType entityType,
         bool? inlinedOwnTypes)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         return entityType.GetNavigations()
                          .Where(n => n.ForeignKey.IsOwnership &&
                                      n.ForeignKey.PrincipalEntityType == entityType &&
                                      (inlinedOwnTypes == null || inlinedOwnTypes == n.IsOwnedTypeInline()));
      }
   }
}
