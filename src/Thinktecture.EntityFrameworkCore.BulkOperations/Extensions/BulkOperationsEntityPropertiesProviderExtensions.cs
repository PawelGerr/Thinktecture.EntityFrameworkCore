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
      public static IReadOnlyList<IProperty> GetPropertiesForTempTable(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetProperties(entityType)).Count == 0
                   ? entityType.GetProperties().ToList()
                   : properties;
      }

      /// <summary>
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to include into a temp table.</returns>
      public static IReadOnlyList<IProperty> GetKeyProperties(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty> properties;

         var keyProperties = entityPropertiesProvider is null || (properties = entityPropertiesProvider.GetProperties(entityType)).Count == 0
                                ? entityType.FindPrimaryKey()?.Properties
                                : properties;

         if (keyProperties is null or { Count: 0 })
            throw new ArgumentException("The number of key properties to perform JOIN/match on cannot be 0.");

         return keyProperties;
      }

      /// <summary>
      /// Determines properties to insert into a (temp) table.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to insert into a (temp) table.</returns>
      public static IReadOnlyList<IProperty> GetPropertiesForInsert(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         return GetWritableProperties(entityPropertiesProvider, entityType);
      }

      /// <summary>
      /// Determines properties to use in update of a table.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to use in update of a table.</returns>
      public static IReadOnlyList<IProperty> GetPropertiesForUpdate(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         return GetWritableProperties(entityPropertiesProvider, entityType);
      }

      private static IReadOnlyList<IProperty> GetWritableProperties(
         IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetProperties(entityType)).Count == 0
                   ? entityType.GetProperties().Where(p => p.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore).ToList()
                   : properties;
      }
   }
}
