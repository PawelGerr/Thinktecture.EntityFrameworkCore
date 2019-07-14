using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;

namespace Thinktecture.Linq.Expressions
{
   /// <summary>
   /// Visitor for searching a <see cref="LambdaExpression"/>.
   /// </summary>
   public class LambdaExpressionSearchingVisitor : ExpressionVisitor
   {
      /// <summary>
      /// An instance of <see cref="LambdaExpressionSearchingVisitor"/>.
      /// </summary>
      public static readonly LambdaExpressionSearchingVisitor Instance = new LambdaExpressionSearchingVisitor();

      /// <summary>
      /// Searches for <see cref="LambdaExpression"/> in provided <paramref name="expression"/>.
      /// </summary>
      /// <param name="expression">Expression to search for lambda in.</param>
      /// <returns>Found instance of <see cref="LambdaExpression"/>.</returns>
      /// <exception cref="ArgumentNullException">Provided <paramref name="expression"/> is null.</exception>
      /// <exception cref="ArgumentException">Provided <paramref name="expression"/> does not contain a lambda.</exception>
      [NotNull]
      public LambdaExpression GetLambda([NotNull] Expression expression)
      {
         if (expression == null)
            throw new ArgumentNullException(nameof(expression));

         var foundExpression = Visit(expression);

         if (foundExpression == null || foundExpression.NodeType != ExpressionType.Lambda)
            throw new ArgumentException("The provided expression does not contain nor point to a lambda expression.");

         return (LambdaExpression)foundExpression;
      }

      /// <inheritdoc />
      protected override Expression VisitLambda<T>(Expression<T> node)
      {
         return node;
      }

      /// <inheritdoc />
      protected override Expression VisitUnary(UnaryExpression node)
      {
         if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.Quote)
            return Visit(node.Operand);

         throw NotSupported(node);
      }

      /// <inheritdoc />
      protected override Expression VisitMember(MemberExpression node)
      {
         var instanceExpression = node.Expression;

         if (instanceExpression.NodeType != ExpressionType.Constant)
            instanceExpression = Visit(instanceExpression);

         if (instanceExpression.NodeType == ExpressionType.Constant)
         {
            var value = GetMemberValue(node, ((ConstantExpression)instanceExpression).Value);

            if (value is Expression exp)
               return exp;

            return Expression.Constant(value);
         }

         throw new NotSupportedException($"The value of most inner expression must be of type '{ExpressionType.Constant}'. Found expression: {node.GetType().DisplayName()}");
      }

      /// <inheritdoc />
      protected override Expression VisitMethodCall(MethodCallExpression node)
      {
         var instanceExpression = Visit(node.Object);

         if (instanceExpression != null && instanceExpression.NodeType != ExpressionType.Constant)
            throw new NotSupportedException($"The expression representing the instance to call the method on must be of type '{ExpressionType.Constant}'. Found expression: {instanceExpression.GetType().DisplayName()}");

         var arguments = node.Arguments.Select(a =>
                                               {
                                                  var visitedArgument = Visit(a);

                                                  if (visitedArgument.NodeType != ExpressionType.Constant)
                                                     throw new NotSupportedException($"All expressions representing the arguments of the method call must be of type '{ExpressionType.Constant}'. Found expression: {instanceExpression.GetType().DisplayName()}");

                                                  return ((ConstantExpression)a).Value;
                                               })
                             .ToArray();

         var value = node.Method.Invoke(((ConstantExpression)instanceExpression)?.Value, arguments);

         if (value is Expression exp)
            return exp;

         return Expression.Constant(value);
      }

      [NotNull]
      private static object GetMemberValue([NotNull] MemberExpression memberAccess, object instance)
      {
         if (memberAccess == null)
            throw new ArgumentNullException(nameof(memberAccess));

         switch (memberAccess.Member.MemberType)
         {
            case MemberTypes.Field:
               var fieldInfo = (FieldInfo)memberAccess.Member;
               return fieldInfo.GetValue(instance);

            case MemberTypes.Property:
               var propertyInfo = (PropertyInfo)memberAccess.Member;
               return propertyInfo.GetValue(instance);

            default:
               throw new NotSupportedException($"Member type '{memberAccess.Member.MemberType}' is not supported.");
         }
      }

      [NotNull]
      private static Exception NotSupported([NotNull] Expression node)
      {
         return new NotSupportedException($"Node of type '{node.GetType().DisplayName()}' is not supported.");
      }
   }
}
