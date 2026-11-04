using System.Diagnostics.CodeAnalysis;
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
   /// Supports direct member access (e.g., <c>e => e.Property</c>), member access through complex properties
   /// (e.g., <c>e => e.ComplexProperty.Property</c>) and <c>EF.Property</c> calls
   /// (e.g., <c>e => EF.Property&lt;int&gt;(e, "_privateField")</c>).
   /// </summary>
   public static PropertyAccessInfo ExtractPropertyAccess(LambdaExpression expression)
   {
      return UnwrapConvert(expression.Body) switch
      {
         MemberExpression memberExpr when TryExtractMemberChain(memberExpr) is { } access => access,
         MethodCallExpression methodCall when IsEfPropertyCall(methodCall) => UnwrapConvert(methodCall.Arguments[0]) is ParameterExpression
                                                                                ? new PropertyAccessInfo(ExtractEfPropertyName(methodCall))
                                                                                : throw new NotSupportedException($"EF.Property is supported only on the lambda parameter, not through a complex property, a navigation or another expression. Expression: {expression}"),
         _ => throw new NotSupportedException($"Only simple property access expressions (e => e.Property), member access through complex properties (e => e.ComplexProperty.Property) or EF.Property calls on the lambda parameter are supported. Expression: {expression}")
      };
   }

   private static PropertyAccessInfo? TryExtractMemberChain(MemberExpression memberExpr)
   {
      if (memberExpr.Member is not (PropertyInfo or FieldInfo))
         return null;

      var complexPath = new List<MemberInfo>();
      var instance = UnwrapConvert(memberExpr.Expression);

      while (instance is MemberExpression { Member: PropertyInfo or FieldInfo } complexMemberExpr)
      {
         complexPath.Insert(0, complexMemberExpr.Member);
         instance = UnwrapConvert(complexMemberExpr.Expression);
      }

      return instance is ParameterExpression ? new PropertyAccessInfo(memberExpr.Member, complexPath) : null;
   }

   /// <summary>
   /// Resolves an <see cref="IProperty"/> from the given entity type using the property access information.
   /// </summary>
   public static IProperty ResolveProperty(IEntityType entityType, PropertyAccessInfo access)
   {
      var declaringType = ResolveDeclaringType(entityType, access);
      var property = access.Member is not null
                        ? declaringType.FindProperty(access.Member)
                        : declaringType.FindProperty(access.PropertyName);

      return property ?? throw new InvalidOperationException(
                $"The property '{access.PropertyName}' is not a mapped property on '{declaringType.DisplayName()}'.");
   }

   /// <summary>
   /// Replaces every entry whose target selects a whole complex property (e.g. <c>e => e.ComplexProperty</c>)
   /// with one entry per member of the complex type, recursively for nested complex types.
   /// The value selector of such an entry is extended by the same member access.
   /// Other entries are returned unchanged.
   /// </summary>
   public static IReadOnlyList<ISetPropertyEntry> ExpandComplexProperties(IEntityType entityType, IReadOnlyList<ISetPropertyEntry> entries)
   {
      var expandedEntries = new List<ISetPropertyEntry>(entries.Count);

      foreach (var entry in entries)
      {
         var countBefore = expandedEntries.Count;
         AddExpandedEntries(entityType, entry, expandedEntries);

         if (expandedEntries.Count == countBefore)
            throw new ArgumentException($"No column can be written, because every member of the selected complex properties is generated (PropertySaveBehavior.Ignore). Expression: {entry.TargetPropertySelector}");
      }

      return expandedEntries;
   }

   private static void AddExpandedEntries(IEntityType entityType, ISetPropertyEntry entry, List<ISetPropertyEntry> expandedEntries)
   {
      var access = ExtractPropertyAccess(entry.TargetPropertySelector);
      var complexProperty = access.Member is null ? null : ResolveDeclaringType(entityType, access).FindComplexProperty(access.Member);

      if (complexProperty is null)
      {
         expandedEntries.Add(entry);
         return;
      }

      if (complexProperty.IsCollection)
         throw new NotSupportedException($"The complex collection '{complexProperty.Name}' is not supported. Expression: {entry.TargetPropertySelector}");

      if (complexProperty.IsNullable)
         throw new NotSupportedException($"The nullable complex property '{complexProperty.Name}' cannot be set as a whole. Expression: {entry.TargetPropertySelector}");

      var complexType = complexProperty.ComplexType;

      foreach (var member in complexType.GetProperties().Concat<IPropertyBase>(complexType.GetComplexProperties()))
      {
         // Skip members that EF Core never writes, e.g. computed columns (same rule as IEntityPropertiesProvider.InsertAndUpdateFilter).
         if (member is IProperty property && property.GetBeforeSaveBehavior() == PropertySaveBehavior.Ignore)
            continue;

         var memberInfo = (MemberInfo?)member.PropertyInfo ?? member.FieldInfo
                          ?? throw new NotSupportedException($"The shadow property '{member.Name}' of the complex property '{complexProperty.Name}' cannot be set through the whole complex property. Expression: {entry.TargetPropertySelector}");

         var memberEntry = new SetPropertyEntry(AppendMemberAccess(entry.TargetPropertySelector, memberInfo, entry.TargetPropertySelector),
                                                AppendMemberAccess(entry.ValueSelector, memberInfo, entry.TargetPropertySelector));

         AddExpandedEntries(entityType, memberEntry, expandedEntries);
      }
   }

   private static ITypeBase ResolveDeclaringType(IEntityType entityType, PropertyAccessInfo access)
   {
      ITypeBase type = entityType;

      foreach (var member in access.ComplexPath)
      {
         var complexProperty = type.FindComplexProperty(member)
                               ?? throw new NotSupportedException($"The member '{member.Name}' is not a complex property of '{type.DisplayName()}'. Only simple property access expressions (e => e.Property), member access through complex properties (e => e.ComplexProperty.Property) or EF.Property calls on the lambda parameter are supported.");

         if (complexProperty.IsNullable)
            throw new NotSupportedException($"The member '{member.Name}' of '{type.DisplayName()}' is a nullable complex property. Members of a nullable complex property cannot be set. Set a non-nullable complex property or one of its members instead.");

         type = complexProperty.ComplexType;
      }

      return type;
   }

   private static LambdaExpression AppendMemberAccess(LambdaExpression lambda, MemberInfo member, LambdaExpression targetSelector)
   {
      var instance = lambda.Body;

      // A selector typed as 'object' boxes the complex value; drop only that conversion, not a real one.
      if (instance is UnaryExpression { NodeType: ExpressionType.Convert } convert && member.DeclaringType!.IsAssignableFrom(convert.Operand.Type))
         instance = convert.Operand;

      if (Nullable.GetUnderlyingType(instance.Type) is { } underlyingType)
         throw new NotSupportedException($"The value of the whole complex property must not be of the nullable type '{underlyingType.Name}?'. Use a non-nullable value. Expression: {targetSelector}");

      return Expression.Lambda(Expression.MakeMemberAccess(instance, member), lambda.Parameters);
   }

   [return: NotNullIfNotNull(nameof(expression))]
   private static Expression? UnwrapConvert(Expression? expression)
   {
      while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.Quote } unary)
      {
         expression = unary.Operand;
      }

      return expression;
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
