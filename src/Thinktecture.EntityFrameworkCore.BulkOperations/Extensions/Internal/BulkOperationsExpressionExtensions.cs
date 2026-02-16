using System.Linq.Expressions;
using System.Reflection;

namespace Thinktecture.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class BulkOperationsExpressionExtensions
{
   /// <summary>
   /// This is an internal API.
   /// </summary>
   public static IReadOnlyList<MemberInfo> ExtractMembers<T, TResult>(this Expression<Func<T, TResult?>> projection)
   {
      ArgumentNullException.ThrowIfNull(projection);

      var members = new List<MemberInfo>();
      ExtractMembers(members, projection.Parameters[0], projection.Body);

      return members;
   }

   private static void ExtractMembers(List<MemberInfo> members, ParameterExpression paramExpression, Expression body)
   {
      while (true)
      {
         switch (body.NodeType)
         {
            case ExpressionType.Convert:
            case ExpressionType.Quote:
               body = ((UnaryExpression)body).Operand;
               continue;

            // entity => entity.Member;
            case ExpressionType.MemberAccess:
               members.Add(ExtractMember(paramExpression, (MemberExpression)body));
               break;

            // entity => new { Prop = entity.Member }
            case ExpressionType.New:
               ExtractMembers(members, paramExpression, (NewExpression)body);
               break;

            // entity => new() { Prop = entity.Member }
            case ExpressionType.MemberInit:
               ExtractMembers(members, paramExpression, (MemberInitExpression)body);
               break;

            default:
               throw new NotSupportedException($"The expression of type '{body.NodeType}' is not supported. Expression: {body}.");
         }

         break;
      }
   }

   private static void ExtractMembers(List<MemberInfo> members, ParameterExpression paramExpression, MemberInitExpression memberInit)
   {
      foreach (var binding in memberInit.Bindings)
      {
         if (binding is not MemberAssignment assignment)
            throw new NotSupportedException($"Object initializer must have member assignments only, but found '{binding.GetType().FullName}'. Current expression: {binding}");

         ExtractMembers(members, paramExpression, assignment.Expression);
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
