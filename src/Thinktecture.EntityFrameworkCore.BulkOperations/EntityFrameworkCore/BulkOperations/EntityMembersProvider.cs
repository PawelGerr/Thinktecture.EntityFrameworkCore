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
      public static IEntityMembersProvider From<T>(Expression<Func<T, object>> projection)
      {
         if (projection == null)
            throw new ArgumentNullException(nameof(projection));

         var members = new List<MemberInfo>();
         ExtractMembers(members, projection.Parameters[0], projection.Body);

         if (members.Count == 0)
            throw new ArgumentException("The provided projection contains no properties.");

         return new EntityMembersProvider(members);
      }

      private static void ExtractMembers(List<MemberInfo> members, ParameterExpression paramExpression, Expression body)
      {
         switch (body.NodeType)
         {
            case ExpressionType.Convert:
            case ExpressionType.Quote:
               ExtractMembers(members, paramExpression, ((UnaryExpression)body).Operand);
               break;

            // entity => entity.Member;
            case ExpressionType.MemberAccess:
               members.Add(ExtractMember(paramExpression, (MemberExpression)body));
               break;

            // entity => new { Prop = entity.Member }
            case ExpressionType.New:
               ExtractMembers(members, paramExpression, (NewExpression)body);
               break;

            default:
               throw new NotSupportedException($"The expression of type '{body.NodeType}' is not supported. Expression: {body}.");
         }
      }

      private static void ExtractMembers(List<MemberInfo> members, ParameterExpression paramExpression, NewExpression newExpression)
      {
         foreach (var argument in newExpression.Arguments)
         {
            ExtractMembers(members, paramExpression, argument);
         }
      }

      // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
      private static MemberInfo ExtractMember(ParameterExpression paramExpression, MemberExpression memberAccess)
      {
         if (memberAccess.Expression != paramExpression)
            throw new NotSupportedException($"Complex projections are not supported. Current expression: {memberAccess}");

         if (memberAccess.Member is PropertyInfo propertyInfo)
            return propertyInfo;

         if (memberAccess.Member is FieldInfo fieldInfo)
            return fieldInfo;

         throw new NotSupportedException($"The projection must have properties and fields only. Current expression: {memberAccess}");
      }
   }
}
