using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="IEntityMembersProvider"/>.
   /// </summary>
   public static class BulkOperationsEntityMembersProviderExtensions
   {
      /// <summary>
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityMembersProvider">Entity member provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to include into a temp table.</returns>
      public static IReadOnlyList<IProperty> GetPropertiesForTempTable(
         this IEntityMembersProvider? entityMembersProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         if (entityMembersProvider == null)
            return entityType.GetProperties().ToList();

         return entityMembersProvider.GetMembers().ConvertToEntityProperties(entityType);
      }

      /// <summary>
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityMembersProvider">Entity member provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to include into a temp table.</returns>
      public static IReadOnlyList<IProperty> GetProperties(
         this IEntityMembersProvider entityMembersProvider,
         IEntityType entityType)
      {
         if (entityMembersProvider == null)
            throw new ArgumentNullException(nameof(entityMembersProvider));
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         return entityMembersProvider.GetMembers().ConvertToEntityProperties(entityType);
      }

      /// <summary>
      /// Determines properties to insert into a (temp) table.
      /// </summary>
      /// <param name="entityMembersProvider">Entity member provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to insert into a (temp) table.</returns>
      public static IReadOnlyList<IProperty> GetPropertiesForInsert(
         this IEntityMembersProvider? entityMembersProvider,
         IEntityType entityType)
      {
         return GetWritableProperties(entityMembersProvider, entityType);
      }

      /// <summary>
      /// Determines properties to use in update of a table.
      /// </summary>
      /// <param name="entityMembersProvider">Entity member provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to use in update of a table.</returns>
      public static IReadOnlyList<IProperty> GetPropertiesForUpdate(
         this IEntityMembersProvider? entityMembersProvider,
         IEntityType entityType)
      {
         return GetWritableProperties(entityMembersProvider, entityType);
      }

      private static IReadOnlyList<IProperty> GetWritableProperties(
         IEntityMembersProvider? entityMembersProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         if (entityMembersProvider == null)
            return entityType.GetProperties().Where(p => p.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore).ToList();

         return entityMembersProvider.GetMembers().ConvertToEntityProperties(entityType);
      }
   }
}
