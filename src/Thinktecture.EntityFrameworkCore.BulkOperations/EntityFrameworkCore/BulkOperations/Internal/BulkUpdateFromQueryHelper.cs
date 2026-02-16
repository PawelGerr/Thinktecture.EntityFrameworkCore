using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class BulkUpdateFromQueryHelper
{
   /// <summary>
   /// Extracts property access information from a simple property access lambda expression.
   /// Supports both direct member access (e.g., <c>e => e.Property</c>) and <c>EF.Property</c> calls
   /// (e.g., <c>e => EF.Property&lt;int&gt;(e, "_privateField")</c>).
   /// </summary>
   public static PropertyAccessInfo ExtractPropertyAccess(LambdaExpression expression)
   {
      var body = expression.Body;

      // Unwrap Convert/Quote
      while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.Quote } unary)
      {
         body = unary.Operand;
      }

      return body switch
      {
         MemberExpression { Expression: ParameterExpression, Member: PropertyInfo or FieldInfo } memberExpr => new PropertyAccessInfo(memberExpr.Member),
         MethodCallExpression methodCall when IsEfPropertyCall(methodCall) => new PropertyAccessInfo(ExtractEfPropertyName(methodCall)),
         _ => throw new NotSupportedException($"Only simple property access expressions or EF.Property calls are supported. Expression: {expression}")
      };
   }

   /// <summary>
   /// Resolves an <see cref="IProperty"/> from the given entity type using the property access information.
   /// </summary>
   public static IProperty ResolveProperty(IEntityType entityType, PropertyAccessInfo access)
   {
      var property = access.Member is not null
                        ? entityType.FindProperty(access.Member)
                        : entityType.FindProperty(access.PropertyName);

      return property ?? throw new InvalidOperationException(
                $"The property '{access.PropertyName}' is not a mapped property on entity type '{entityType.Name}'.");
   }

   private static bool IsEfPropertyCall(MethodCallExpression methodCall)
   {
      return methodCall.Method.DeclaringType == typeof(EF)
             && methodCall.Method.Name == nameof(EF.Property);
   }

   private static string ExtractEfPropertyName(MethodCallExpression methodCall)
   {
      return methodCall.Arguments[1] is ConstantExpression { Value: string propertyName }
                ? propertyName
                : throw new NotSupportedException($"EF.Property calls must use a constant string for the property name. Expression: {methodCall}");
   }
}
