using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
      public static IReadOnlyList<IProperty> GetPropertiesForTempTable(this IEntityMembersProvider? entityMembersProvider,
                                                                       IEntityType entityType)
      {
         if (entityMembersProvider == null)
            return entityType.GetProperties().ToList();

         return ConvertToEntityProperties(entityMembersProvider.GetMembers(), entityType);
      }

      /// <summary>
      /// Determines properties to insert into a (temp) table.
      /// </summary>
      /// <param name="entityMembersProvider">Entity member provider.</param>
      /// <param name="entityType">Entity type.</param>
      /// <returns>Properties to use insert into a (temp) table.</returns>
      public static IReadOnlyList<IProperty> GetPropertiesForInsert(this IEntityMembersProvider? entityMembersProvider,
                                                                    IEntityType entityType)
      {
         if (entityMembersProvider == null)
            return entityType.GetProperties().Where(p => p.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore).ToList();

         return ConvertToEntityProperties(entityMembersProvider.GetMembers(), entityType);
      }

      private static IReadOnlyList<IProperty> ConvertToEntityProperties(IReadOnlyList<MemberInfo> memberInfos, IEntityType entityType)
      {
         var properties = new IProperty[memberInfos.Count];

         for (var i = 0; i < memberInfos.Count; i++)
         {
            var memberInfo = memberInfos[i];
            var property = FindProperty(entityType, memberInfo);

            properties[i] = property ?? throw new ArgumentException($"The member '{memberInfo.Name}' has not found on entity '{entityType.Name}'.", nameof(memberInfos));
         }

         return properties;
      }

      private static IProperty? FindProperty(IEntityType entityType, MemberInfo memberInfo)
      {
         foreach (var property in entityType.GetProperties())
         {
            if (property.PropertyInfo == memberInfo || property.FieldInfo == memberInfo)
               return property;
         }

         return null;
      }
   }
}
