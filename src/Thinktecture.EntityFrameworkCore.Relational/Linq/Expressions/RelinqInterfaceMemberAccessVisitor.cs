using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;

namespace Thinktecture.Linq.Expressions
{
   /// <summary>
   /// Searches for conversions from a (derived) type to an interface
   /// and rewrites member access expressions so the property of the derived type is used instead the one of the base type.
   /// </summary>
   public class RelinqInterfaceMemberAccessVisitor : ExpressionVisitor
   {
      /// <summary>
      /// An instance of <see cref="RelinqInterfaceMemberAccessVisitor"/>.
      /// </summary>
      public static readonly RelinqInterfaceMemberAccessVisitor Instance = new RelinqInterfaceMemberAccessVisitor();

      /// <inheritdoc />
      protected override Expression VisitMember(MemberExpression node)
      {
         if (node.Expression.NodeType == ExpressionType.Convert)
         {
            var conversion = (UnaryExpression)node.Expression;

            if (conversion.Type.IsAssignableFrom(conversion.Operand.Type))
            {
               MemberInfo member = null;

               if (node.Member.MemberType == MemberTypes.Property)
                  member = FindProperty(conversion.Operand.Type, (PropertyInfo)node.Member);

               if (member == null)
                  throw new Exception($"Member with name '{node.Member.Name}' not found in '{conversion.Operand.Type.DisplayName()}'.");

               var operand = Visit(conversion.Operand);
               return Expression.MakeMemberAccess(operand, member);
            }
         }

         return base.VisitMember(node);
      }

      [CanBeNull]
      private static MemberInfo FindProperty([NotNull] Type type, [NotNull] PropertyInfo baseTypeMember)
      {
         return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(p => p.GetMethod == baseTypeMember.GetMethod || p.SetMethod == baseTypeMember.SetMethod);
      }
   }
}
