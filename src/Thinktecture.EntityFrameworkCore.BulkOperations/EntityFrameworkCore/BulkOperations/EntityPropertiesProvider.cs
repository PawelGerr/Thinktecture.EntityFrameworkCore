using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Provides entity properties to work with.
   /// </summary>
   public sealed class EntityPropertiesProvider : IEntityPropertiesProvider
   {
      private readonly IReadOnlyList<MemberInfo> _members;
      private IReadOnlyList<IProperty>? _properties;

      /// <summary>
      /// Initializes new instance of <see cref="EntityPropertiesProvider"/>.
      /// </summary>
      /// <param name="members">Members to use.</param>
      public EntityPropertiesProvider(IReadOnlyList<MemberInfo> members)
      {
         if (members == null)
            throw new ArgumentNullException(nameof(members));
         if (members.Count == 0)
            throw new ArgumentException("The members collection cannot be empty.");

         _members = members;
      }

      /// <inheritdoc />
      public IReadOnlyList<IProperty> GetProperties(IEntityType entityType)
      {
         return _properties ??= _members.ConvertToEntityProperties(entityType);
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

         if (members.Count == 0)
            throw new ArgumentException("The provided projection contains no properties.");

         return new EntityPropertiesProvider(members);
      }
   }
}
