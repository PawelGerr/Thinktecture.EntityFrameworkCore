using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   /// <summary>
   /// Gets entity members provided via the constructor.
   /// </summary>
   public sealed class EntityMembersProvider : IEntityMembersProvider
   {
      private readonly IReadOnlyList<MemberInfo> _members;

      /// <summary>
      /// Initializes new instance of <see cref="EntityMembersProvider"/>.
      /// </summary>
      /// <param name="members">Members to return by the method <see cref="GetMembers"/>.</param>
      public EntityMembersProvider(IReadOnlyList<MemberInfo> members)
      {
         _members = members ?? throw new ArgumentNullException(nameof(members));
      }

      /// <inheritdoc />
      public IReadOnlyList<MemberInfo> GetMembers()
      {
         return _members;
      }

      /// <summary>
      /// Extracts members from the provided <paramref name="projection"/>.
      /// </summary>
      /// <param name="projection">Projection to extract the members from.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IEntityMembersProvider"/> containing members extracted from <paramref name="projection"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">No members couldn't be extracted.</exception>
      /// <exception cref="NotSupportedException">The <paramref name="projection"/> contains unsupported expressions.</exception>
      public static IEntityMembersProvider From<T>(Expression<Func<T, object?>> projection)
      {
         if (projection == null)
            throw new ArgumentNullException(nameof(projection));

         var members = projection.ExtractMembers();

         if (members.Count == 0)
            throw new ArgumentException("The provided projection contains no properties.");

         return new EntityMembersProvider(members);
      }
   }
}
