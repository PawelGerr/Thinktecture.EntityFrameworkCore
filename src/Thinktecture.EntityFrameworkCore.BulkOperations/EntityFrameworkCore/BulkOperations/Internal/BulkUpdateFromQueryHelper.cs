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
   /// Resolves the target property of every entry. An entry whose target selects a whole complex property (e.g. <c>e => e.ComplexProperty</c>)
   /// is replaced by one entry per member of the complex type, recursively for nested complex types, including the discriminator.
   /// The value selector of such an entry is extended by the same member access.
   /// A conditional or coalesce value is split per branch, so the member access is applied to each branch.
   /// </summary>
   public static IReadOnlyList<ResolvedSetPropertyEntry> ExpandComplexProperties(IEntityType entityType, IReadOnlyList<ISetPropertyEntry> entries)
   {
      var expandedEntries = new List<ResolvedSetPropertyEntry>(entries.Count);

      foreach (var entry in entries)
      {
         var access = ExtractPropertyAccess(entry.TargetPropertySelector);
         var complexProperty = access.Member is null ? null : ResolveDeclaringType(entityType, access).FindComplexProperty(access.Member);

         if (complexProperty is null)
         {
            expandedEntries.Add(new ResolvedSetPropertyEntry(ResolveProperty(entityType, access), entry.TargetPropertySelector, entry.ValueSelector));
            continue;
         }

         var countBefore = expandedEntries.Count;
         AddMemberEntries(complexProperty, entry.ValueSelector, entry.TargetPropertySelector, isNullableChain: false, expandedEntries);

         if (expandedEntries.Count == countBefore)
            throw new ArgumentException($"No column can be written, because every member of the selected complex properties is generated (PropertySaveBehavior.Ignore). Expression: {entry.TargetPropertySelector}");
      }

      return expandedEntries;
   }

   private static void AddMemberEntries(
      IComplexProperty complexProperty,
      LambdaExpression valueSelector,
      LambdaExpression targetSelector,
      bool isNullableChain,
      List<ResolvedSetPropertyEntry> expandedEntries)
   {
      if (complexProperty.IsCollection)
         throw new NotSupportedException($"The complex collection '{complexProperty.Name}' is not supported. Expression: {targetSelector}");

      var complexType = complexProperty.ComplexType;
      var guardNull = isNullableChain || complexProperty.IsNullable;
      var discriminator = complexType.FindDiscriminatorProperty();

      foreach (var property in complexType.GetProperties())
      {
         // Skip members that EF Core never writes, e.g. computed columns (same rule as IEntityPropertiesProvider.InsertAndUpdateFilter).
         if (property.GetBeforeSaveBehavior() == PropertySaveBehavior.Ignore)
            continue;

         var memberValue = property == discriminator
                              ? BuildDiscriminatorValue(valueSelector, property, complexType)
                              : AppendMemberAccess(valueSelector, GetMemberInfo(property, complexProperty, targetSelector), guardNull);

         expandedEntries.Add(new ResolvedSetPropertyEntry(property, targetSelector, memberValue));
      }

      foreach (var nestedProperty in complexType.GetComplexProperties())
      {
         var nestedValueSelector = AppendMemberAccess(valueSelector, GetMemberInfo(nestedProperty, complexProperty, targetSelector), guardNull);
         AddMemberEntries(nestedProperty, nestedValueSelector, targetSelector, guardNull, expandedEntries);
      }
   }

   private static MemberInfo GetMemberInfo(IPropertyBase member, IComplexProperty complexProperty, LambdaExpression targetSelector)
   {
      return (MemberInfo?)member.PropertyInfo ?? member.FieldInfo
             ?? throw new NotSupportedException($"The shadow property '{member.Name}' of the complex property '{complexProperty.Name}' cannot be set through the whole complex property. Expression: {targetSelector}");
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

   private static LambdaExpression AppendMemberAccess(LambdaExpression lambda, MemberInfo member, bool guardNull)
   {
      var body = BuildPerBranch(GetComplexValue(lambda, member.DeclaringType!), instance => BuildMemberAccess(instance, member, guardNull));
      return Expression.Lambda(body, lambda.Parameters);
   }

   private static Expression BuildMemberAccess(Expression instance, MemberInfo member, bool guardNull)
   {
      var memberAccess = Expression.MakeMemberAccess(UnwrapNullable(instance), member);

      // ClosureFlatteningVisitor compiles a client-evaluated value as a whole, so this guard follows C# semantics and never dereferences null.
      // A source column keeps the plain member access, because EF Core translates it to the column without a null check.
      if (guardNull && IsClientEvaluated(instance) && !IsNonNullableValueType(instance.Type))
      {
         var nullableType = MakeNullable(memberAccess.Type);
         return Expression.Condition(Expression.Equal(instance, Expression.Constant(null, instance.Type)),
                                     Expression.Default(nullableType),
                                     Expression.Convert(memberAccess, nullableType));
      }

      return memberAccess;
   }

   private static LambdaExpression BuildDiscriminatorValue(LambdaExpression valueSelector, IProperty discriminator, IComplexType complexType)
   {
      var body = BuildPerBranch(GetComplexValue(valueSelector, complexType.ClrType), instance => BuildDiscriminator(instance, discriminator, complexType));
      return Expression.Lambda(body, valueSelector.Parameters);
   }

   private static Expression BuildDiscriminator(Expression instance, IProperty discriminator, IComplexType complexType)
   {
      if (!IsClientEvaluated(instance))
         return Expression.Call(typeof(EF), nameof(EF.Property), [discriminator.ClrType], UnwrapNullable(instance), Expression.Constant(discriminator.Name));

      var nullableType = MakeNullable(discriminator.ClrType);
      var discriminatorValue = Expression.Constant(complexType.GetDiscriminatorValue(), nullableType);

      return IsNonNullableValueType(instance.Type)
                ? discriminatorValue
                : Expression.Condition(Expression.Equal(instance, Expression.Constant(null, instance.Type)),
                                       Expression.Constant(null, nullableType),
                                       discriminatorValue);
   }

   /// <summary>
   /// Splits a conditional or coalesce over complex values per branch, so every leaf is a source or target column, a client value or a discriminator constant.
   /// A client-evaluated test selects the branch in ClosureFlatteningVisitor; a server test becomes one CASE per member column.
   /// </summary>
   private static Expression BuildPerBranch(Expression instance, Func<Expression, Expression> buildLeaf)
   {
      return instance switch
      {
         ConditionalExpression c => MakeConditional(c.Test, BuildPerBranch(c.IfTrue, buildLeaf), BuildPerBranch(c.IfFalse, buildLeaf)),
         // A column as left operand would need a server null test on a complex value, which the value translation does not support.
         BinaryExpression { NodeType: ExpressionType.Coalesce } c when !IsClientEvaluated(c.Left)
            => throw new NotSupportedException($"The null-coalescing operator (??) is supported for a complex value only when its left operand is evaluated on the client, for example a captured variable. Use a conditional with a client-evaluated test, or update the rows in a separate statement. Expression: {c}"),
         // `a ?? b` is `a == null ? b : a`
         BinaryExpression { NodeType: ExpressionType.Coalesce } c => MakeConditional(Expression.Equal(c.Left, Expression.Constant(null, c.Left.Type)),
                                                                                     BuildPerBranch(c.Right, buildLeaf),
                                                                                     BuildPerBranch(c.Left, buildLeaf)),
         _ => buildLeaf(instance)
      };
   }

   private static Expression MakeConditional(Expression test, Expression ifTrue, Expression ifFalse)
   {
      // A guarded client leaf is nullable, a column leaf is not; lift both to the nullable type.
      var type = ifTrue.Type == ifFalse.Type ? ifTrue.Type : MakeNullable(ifTrue.Type);
      return Expression.Condition(test, ConvertIfNeeded(ifTrue, type), ConvertIfNeeded(ifFalse, type));
   }

   private static Expression ConvertIfNeeded(Expression expression, Type type)
   {
      return expression.Type == type ? expression : Expression.Convert(expression, type);
   }

   private static Expression GetComplexValue(LambdaExpression lambda, Type complexClrType)
   {
      var instance = lambda.Body;

      // A selector typed as 'object' boxes the complex value; drop only that conversion, not a real one.
      if (instance is UnaryExpression { NodeType: ExpressionType.Convert } convert
          && complexClrType.IsAssignableFrom(Nullable.GetUnderlyingType(convert.Operand.Type) ?? convert.Operand.Type))
         instance = convert.Operand;

      return instance;
   }

   private static Expression UnwrapNullable(Expression instance)
   {
      return Nullable.GetUnderlyingType(instance.Type) is null ? instance : Expression.Property(instance, nameof(Nullable<int>.Value));
   }

   private static bool IsNonNullableValueType(Type type)
   {
      return type.IsValueType && Nullable.GetUnderlyingType(type) is null;
   }

   private static Type MakeNullable(Type type)
   {
      return IsNonNullableValueType(type) ? typeof(Nullable<>).MakeGenericType(type) : type;
   }

   private static bool IsClientEvaluated(Expression expression)
   {
      var finder = new ParameterFinder();
      finder.Visit(expression);

      return !finder.HasParameter;
   }

   private sealed class ParameterFinder : ExpressionVisitor
   {
      public bool HasParameter { get; private set; }

      protected override Expression VisitParameter(ParameterExpression node)
      {
         HasParameter = true;
         return node;
      }
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
