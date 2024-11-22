using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.Linq.Expressions;

/// <summary>
/// Visitor for searching a <see cref="LambdaExpression"/>.
/// </summary>
public class LambdaExpressionSearchingVisitor : ExpressionVisitor
{
   private static readonly LambdaExpressionSearchingVisitor _instance = new();

   /// <summary>
   /// Searches for <see cref="LambdaExpression"/> in provided <paramref name="expression"/>.
   /// </summary>
   /// <param name="expression">Expression to search for lambda in.</param>
   /// <returns>Found instance of <see cref="LambdaExpression"/>.</returns>
   /// <exception cref="ArgumentNullException">Provided <paramref name="expression"/> is null.</exception>
   /// <exception cref="ArgumentException">Provided <paramref name="expression"/> does not contain a lambda.</exception>
   public static LambdaExpression GetLambda(Expression expression)
   {
      ArgumentNullException.ThrowIfNull(expression);

      var foundExpression = _instance.Visit(expression);

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
      ArgumentNullException.ThrowIfNull(node);

      if (node.NodeType is ExpressionType.Convert or ExpressionType.Quote)
         return Visit(node.Operand) ?? throw NotSupported(node);

      throw NotSupported(node);
   }

   /// <inheritdoc />
   protected override Expression VisitMember(MemberExpression node)
   {
      ArgumentNullException.ThrowIfNull(node);

      var instanceExpression = node.Expression;

      if (instanceExpression is { NodeType: not ExpressionType.Constant })
         instanceExpression = Visit(instanceExpression);

      if (instanceExpression?.NodeType == ExpressionType.Constant)
      {
         var instance = ((ConstantExpression)instanceExpression).Value
                        ?? throw new Exception($"Instance cannot be null. Member: {node.Member}.");

         var value = GetMemberValue(node, instance);

         if (value is Expression exp)
            return exp;

         return Expression.Constant(value);
      }

      throw new NotSupportedException($"The value of most inner expression must be of type '{ExpressionType.Constant}'. Found expression: {node.GetType().ShortDisplayName()}");
   }

   /// <inheritdoc />
   protected override Expression VisitMethodCall(MethodCallExpression node)
   {
      ArgumentNullException.ThrowIfNull(node);

      var instanceExpression = Visit(node.Object);

      if (instanceExpression is { NodeType: not ExpressionType.Constant })
         throw new NotSupportedException($"The expression representing the instance to call the method on must be of type '{ExpressionType.Constant}'. Found expression: {instanceExpression.GetType().ShortDisplayName()}");

      var arguments = node.Arguments.Select(a =>
                                            {
                                               var visitedArgument = Visit(a);

                                               if (visitedArgument is null)
                                                  throw new NotSupportedException($"The expressions representing the arguments of the method call could not be translated. Found expression: {a}");

                                               if (visitedArgument.NodeType != ExpressionType.Constant)
                                                  throw new NotSupportedException($"All expressions representing the arguments of the method call must be of type '{ExpressionType.Constant}'. Found expression: {visitedArgument.GetType().ShortDisplayName()}");

                                               return ((ConstantExpression)a).Value;
                                            })
                          .ToArray();

      var value = node.Method.Invoke(((ConstantExpression?)instanceExpression)?.Value, arguments);

      if (value is Expression exp)
         return exp;

      return Expression.Constant(value);
   }

   private static object? GetMemberValue(MemberExpression memberAccess, object instance)
   {
      ArgumentNullException.ThrowIfNull(memberAccess);

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

   private static Exception NotSupported(Expression node)
   {
      return new NotSupportedException($"Node of type '{node.GetType().ShortDisplayName()}' is not supported.");
   }
}
