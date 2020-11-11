using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace Thinktecture.Linq.Expressions
{
   /// <summary>
   /// Searches for conversions from a (derived) type to an interface
   /// and rewrites member access expressions so the property of the derived type is used instead the one of the interface.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public class RelinqInterfaceMemberAccessVisitor : ExpressionVisitor
   {
      private static readonly RelinqInterfaceMemberAccessVisitor _instance = new RelinqInterfaceMemberAccessVisitor();

      /// <summary>
      /// Rewrites the provided <paramref name="expression"/> so the property of the concrete implementation type is used instead of an interface.
      /// </summary>
      /// <param name="expression">Expression to rewrite.</param>
      /// <typeparam name="T">Type of the expression.</typeparam>
      /// <returns>
      /// A rewritten <paramref name="expression"/> if it contains property-access to an interface;
      /// otherwise the provided <paramref name="expression"/>.
      /// </returns>
      /// <exception cref="NotSupportedException">
      /// The provided <paramref name="expression"/> could not be rewritten.
      /// </exception>
      public static T Rewrite<T>(T expression)
         where T : Expression
      {
         var visitedExpression = _instance.Visit(expression);

         if (visitedExpression is T exp)
            return exp;

         throw new NotSupportedException($"The provided expression could not be rewritten: {expression}");
      }

      /// <inheritdoc />
      protected override Expression VisitMember(MemberExpression node)
      {
         if (node == null)
            throw new ArgumentNullException(nameof(node));

         if (node.Expression.NodeType == ExpressionType.Convert)
         {
            var conversion = (UnaryExpression)node.Expression;

            if (conversion.Type.IsInterface && conversion.Type.IsAssignableFrom(conversion.Operand.Type))
            {
               MemberInfo? member = null;

               if (node.Member.MemberType == MemberTypes.Property)
                  member = FindProperty(conversion.Operand.Type, conversion.Type, (PropertyInfo)node.Member);

               if (member == null)
                  throw new MissingMemberException(conversion.Operand.Type.ShortDisplayName(), node.Member.Name);

               var operand = Visit(conversion.Operand);
               return Expression.MakeMemberAccess(operand, member);
            }
         }

         return base.VisitMember(node);
      }

      private static MemberInfo? FindProperty(Type dstType, Type interfaceType, PropertyInfo interfaceMember)
      {
         var map = dstType.GetInterfaceMap(interfaceType);
         var targetMethod = FindTargetMethod(map, interfaceMember);

         if (targetMethod != null)
         {
            return dstType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          .FirstOrDefault(p => p.GetMethod == targetMethod || p.SetMethod == targetMethod);
         }

         return null;
      }

      private static MethodInfo? FindTargetMethod(InterfaceMapping map, PropertyInfo interfaceProperty)
      {
         for (var i = 0; i < map.InterfaceMethods.Length; i++)
         {
            var interfaceMethod = map.InterfaceMethods[i];

            if (interfaceMethod == interfaceProperty.GetMethod || interfaceMethod == interfaceProperty.SetMethod)
               return map.TargetMethods[i];
         }

         return null;
      }
   }
}
