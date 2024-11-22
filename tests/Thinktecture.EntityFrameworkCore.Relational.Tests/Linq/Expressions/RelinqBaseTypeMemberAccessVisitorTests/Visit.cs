using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Thinktecture.Linq.Expressions.RelinqBaseTypeMemberAccessVisitorTests;

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
public class Visit
{
   [Fact]
   public void Should_not_change_expression_without_conversion()
   {
      Expression<Func<MyImplicitImplementingObject, string?>> expression = o => o.MyProperty;

      var visitedExpression = RelinqInterfaceMemberAccessVisitor.Rewrite(expression);

      visitedExpression.Should().Be(expression);
   }

   [Fact]
   public void Should_change_property_expression_of_implicit_implemented_interface_to_one_of_concrete_type()
   {
      var expression = GetInterfaceImplementingExpression<MyImplicitImplementingObject>();

      var visitedExpression = RelinqInterfaceMemberAccessVisitor.Rewrite(expression);

      var memberExpression = visitedExpression.As<LambdaExpression>().Body.As<MemberExpression>();

      memberExpression.Should().NotBeNull();
      memberExpression.Member.Should().Be(typeof(MyImplicitImplementingObject).GetProperty(nameof(MyImplicitImplementingObject.MyProperty), BindingFlags.Instance | BindingFlags.Public));
      memberExpression.Expression.Should().Be(expression.Parameters[0]);
   }

   [Fact]
   public void Should_change_property_expression_of_implicit_implemented_interface_with_setter_to_one_of_concrete_type()
   {
      var expression = GetInterfaceImplementingExpression<MyImplicitImplementingObjectWithSetter>();

      var visitedExpression = RelinqInterfaceMemberAccessVisitor.Rewrite(expression);

      var memberExpression = visitedExpression.As<LambdaExpression>().Body.As<MemberExpression>();

      memberExpression.Should().NotBeNull();
      memberExpression.Member.Should().Be(typeof(MyImplicitImplementingObjectWithSetter).GetProperty(nameof(MyImplicitImplementingObjectWithSetter.MyProperty), BindingFlags.Instance | BindingFlags.Public));
      memberExpression.Expression.Should().Be(expression.Parameters[0]);
   }

   [Fact]
   public void Should_change_property_expression_of_explicit_implemented_interface_to_one_of_concrete_type()
   {
      var expression = GetInterfaceImplementingExpression<MyExplicitImplementingObject>();

      var visitedExpression = RelinqInterfaceMemberAccessVisitor.Rewrite(expression);

      var memberExpression = visitedExpression.As<LambdaExpression>().Body.As<MemberExpression>();

      memberExpression.Should().NotBeNull();
      memberExpression.Member.Should().NotBeNull().And.Subject
                      .Should().Be(typeof(MyExplicitImplementingObject).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault());
      memberExpression.Expression.Should().Be(expression.Parameters[0]);
   }

   [Fact]
   public void Should_pick_correct_property_if_interface_is_implemented_explicit_and_implicit()
   {
      var expression = GetInterfaceImplementingExpression<MyDualImplementingObject>();

      var visitedExpression = RelinqInterfaceMemberAccessVisitor.Rewrite(expression);

      var memberExpression = visitedExpression.As<LambdaExpression>().Body.As<MemberExpression>();

      memberExpression.Should().NotBeNull();
      memberExpression.Member.Should().NotBeNull().And.Subject
                      .Should().Be(typeof(MyDualImplementingObject).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault())
                      .Should().NotBe(typeof(MyDualImplementingObject).GetProperty(nameof(MyDualImplementingObject.MyProperty), BindingFlags.Instance | BindingFlags.NonPublic));
      memberExpression.Expression.Should().Be(expression.Parameters[0]);
   }

   private static Expression<Func<T, string?>> GetInterfaceImplementingExpression<T>()
      where T : IMyObject
   {
      return o => o.MyProperty;
   }

   private interface IMyObject
   {
      string? MyProperty { get; }
   }

   private class MyImplicitImplementingObject : IMyObject
   {
      public string? MyProperty { get; }
   }

   private class MyImplicitImplementingObjectWithSetter : IMyObject
   {
      public string? MyProperty { get; set; }
   }

   private class MyExplicitImplementingObject : IMyObject
   {
      string? IMyObject.MyProperty { get; }
   }

   private class MyDualImplementingObject : IMyObject
   {
      string? IMyObject.MyProperty { get; }

      public string? MyProperty { get; }
   }
}
