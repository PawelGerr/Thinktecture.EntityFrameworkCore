using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="INavigation"/>.
   /// </summary>
   public static class RelationalNavigationExtensions
   {
      /// <summary>
      /// Indication whether the owned type is persisted in the same table as the owner.
      /// </summary>
      /// <param name="navigation">Navigation pointing to the owned type.</param>
      /// <returns><c>true</c> if the owned type is persisted in the same table as the owner; otherwise <c>false</c>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="navigation"/> is <c>null</c>.
      /// </exception>
      public static bool IsOwnedTypeInline(
         this INavigation navigation)
      {
         if (navigation == null)
            throw new ArgumentNullException(nameof(navigation));

         var sourceType = navigation.DeclaringEntityType;
         var targetType = navigation.TargetEntityType;

         return sourceType.GetSchema() == targetType.GetSchema() &&
                sourceType.GetTableName() == targetType.GetTableName();
      }
   }
}
