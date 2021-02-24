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
      /// <param name="properties">Properties of the table.</param>
      /// <returns>Properties to include into a temp table.</returns>
      public static IReadOnlyList<IProperty> GetKeyProperties(
         this IEntityMembersProvider? entityMembersProvider,
         IEntityType entityType,
         IReadOnlyList<IProperty> properties)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         var keyProperties = entityMembersProvider is null
                                ? entityType.FindPrimaryKey()?.Properties
                                : entityMembersProvider.GetProperties(entityType);

         if (keyProperties is null or { Count: 0 })
            throw new ArgumentException("The number of key properties to perform JOIN/match on cannot be 0.");

         var missingColumns = keyProperties.Except(properties);

         if (missingColumns.Any())
         {
            throw new InvalidOperationException(@$"Not all key properties are part of the source table.
Missing columns: {String.Join(", ", missingColumns.Select(c => c.GetColumnBaseName()))}.");
         }

         return keyProperties;
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
