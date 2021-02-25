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
      public static IReadOnlyList<IProperty> DeterminePropertiesForTempTable(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetPropertiesForTempTable(entityType)).Count == 0
                   ? entityType.GetProperties().ToList()
                   : properties;
      }

      /// <summary>
      /// Determines properties to include into a temp table into.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to include into a temp table.</returns>
      public static IReadOnlyList<IProperty> DetermineKeyProperties(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty>? properties;

         if (entityPropertiesProvider is null || (properties = entityPropertiesProvider.GetKeyProperties(entityType)).Count == 0)
         {
            properties = entityType.FindPrimaryKey()?.Properties;

            if (properties is null or { Count: 0 })
               throw new InvalidOperationException($"The entity '{entityType.Name}' has no primary key. Please provide key properties to perform JOIN/match on.");
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
      /// <returns>Properties to insert into a (temp) table.</returns>
      public static IReadOnlyList<IProperty> DeterminePropertiesForInsert(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetPropertiesForInsert(entityType)).Count == 0
                   ? entityType.GetProperties().Where(p => p.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore).ToList()
                   : properties;
      }

      /// <summary>
      /// Determines properties to use in update of a table.
      /// </summary>
      /// <param name="entityPropertiesProvider">Entity properties provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to use in update of a table.</returns>
      public static IReadOnlyList<IProperty> DeterminePropertiesForUpdate(
         this IEntityPropertiesProvider? entityPropertiesProvider,
         IEntityType entityType)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         IReadOnlyList<IProperty> properties;

         return entityPropertiesProvider == null || (properties = entityPropertiesProvider.GetPropertiesForUpdate(entityType)).Count == 0
                   ? entityType.GetProperties().Where(p => p.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore).ToList()
                   : properties;
      }
   }
}
