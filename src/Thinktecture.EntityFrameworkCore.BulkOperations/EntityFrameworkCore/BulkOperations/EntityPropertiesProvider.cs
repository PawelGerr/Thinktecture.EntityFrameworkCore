using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Provides entity properties to work with.
   /// </summary>
   public sealed class EntityPropertiesProvider : IEntityPropertiesProvider
   {
      private readonly IReadOnlyList<MemberInfo> _members;

      /// <summary>
      /// <see cref="IEntityPropertiesProvider"/> with 0 properties.
      /// </summary>
      public static readonly IEntityPropertiesProvider Empty = new EntityPropertiesProvider(Array.Empty<MemberInfo>());

      /// <summary>
      /// Initializes new instance of <see cref="EntityPropertiesProvider"/>.
      /// </summary>
      /// <param name="members">Members to use.</param>
      public EntityPropertiesProvider(IReadOnlyList<MemberInfo> members)
      {
         _members = members ?? throw new ArgumentNullException(nameof(members));
      }

      /// <inheritdoc />
      public IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes, filter);
      }

      /// <inheritdoc />
      public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes, filter);
      }

      /// <inheritdoc />
      public IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes, filter);
      }

      /// <inheritdoc />
      public IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes, filter);
      }

      private IReadOnlyList<PropertyWithNavigations> GetProperties(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return _members.ConvertToEntityProperties(entityType, inlinedOwnTypes, filter);
      }

      /// <summary>
      /// Extracts members from the provided <paramref name="projection"/>.
      /// </summary>
      /// <param name="projection">Projection to extract the members from.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IEntityPropertiesProvider"/> containing members extracted from <paramref name="projection"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">No members couldn't be extracted.</exception>
      /// <exception cref="NotSupportedException">The <paramref name="projection"/> contains unsupported expressions.</exception>
      public static IEntityPropertiesProvider From<T>(Expression<Func<T, object?>> projection)
      {
         if (projection == null)
            throw new ArgumentNullException(nameof(projection));

         var members = projection.ExtractMembers();

         return members.Count == 0 ? Empty : new EntityPropertiesProvider(members);
      }
   }
}
