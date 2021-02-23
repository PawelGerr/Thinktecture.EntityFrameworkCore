using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="Expression"/>.
   /// </summary>
   public static class BulkOperationsExpressionExtensions
   {
      internal static IReadOnlyList<MemberInfo> ExtractMembers<T>(this Expression<Func<T, object?>> projection)
      {
         if (projection == null)
            throw new ArgumentNullException(nameof(projection));

         var members = new List<MemberInfo>();
         ExtractMembers(members, projection.Parameters[0], projection.Body);

         return members;
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
