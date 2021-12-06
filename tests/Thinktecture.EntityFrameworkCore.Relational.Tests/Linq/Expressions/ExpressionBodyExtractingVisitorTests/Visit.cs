using System;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Thinktecture.Linq.Expressions.ExpressionBodyExtractingVisitorTests;

public class Visit
{
   private static readonly PropertyInfo _myOtherProperty = typeof(MyObject).GetProperty(nameof(MyObject.MyOtherProperty), BindingFlags.Instance | BindingFlags.Public)
                                                           ?? throw new Exception($"The property {nameof(MyObject.MyOtherProperty)} not found.");

   [Fact]
   public void Should_return_the_same_expression_if_no_ExtractBody_found()
   {
      Expression<Func<MyObject, bool>> expression = o => o.MyProperty == null;

      var visitedExpression = ExpressionBodyExtractingVisitor.Rewrite(expression);

      visitedExpression.Should().Be(expression);
   }

   [Fact]
   public void Should_replace_ExtractBody_with_lambda_body_provided_as_field()
   {
      Expression<Func<MyObject, string?>> extractFromExpression = o => o.MyOtherProperty;
      Expression<Func<MyObject, bool>> expression = o => o.MyProperty == extractFromExpression.ExtractBody(o);

      var visitedExpression = ExpressionBodyExtractingVisitor.Rewrite(expression);

      ValidateVisitedExpressions(expression, visitedExpression);
   }

   [Fact]
   public void Should_replace_ExtractBody_with_lambda_body_provided_as_property()
   {
      Expression<Func<MyObject, string?>> extractFromExpression = o => o.MyOtherProperty;
      var extractExpressionHolder = new { Expr = extractFromExpression };
      Expression<Func<MyObject, bool>> expression = o => o.MyProperty == extractExpressionHolder.Expr.ExtractBody(o);

      var visitedExpression = ExpressionBodyExtractingVisitor.Rewrite(expression);

      ValidateVisitedExpressions(expression, visitedExpression);
   }

   [Fact]
   public void Should_replace_ExtractBody_with_lambda_body_provided_inline()
   {
      Expression<Func<MyObject, bool>> expression = o => o.MyProperty == ((Expression<Func<MyObject, string?>>)(i => i.MyOtherProperty)).ExtractBody(o);

      var visitedExpression = ExpressionBodyExtractingVisitor.Rewrite(expression);

      ValidateVisitedExpressions(expression, visitedExpression);
   }

   [Fact]
   public void Should_replace_ExtractBody_with_lambda_body_provided_as_static_method_returnvalue()
   {
      Expression<Func<MyObject, bool>> expression = o => o.MyProperty == MyObject.GetExpressionFromStaticMethod().ExtractBody(o);

      var visitedExpression = ExpressionBodyExtractingVisitor.Rewrite(expression);

      ValidateVisitedExpressions(expression, visitedExpression);
   }

   [Fact]
   public void Should_replace_ExtractBody_with_lambda_body_provided_as_instance_method_returnvalue()
   {
      var myObj = new MyObject();
      Expression<Func<MyObject, bool>> expression = o => o.MyProperty == myObj.GetExpressionFromInstanceMethod().ExtractBody(o);

      var visitedExpression = ExpressionBodyExtractingVisitor.Rewrite(expression);

      ValidateVisitedExpressions(expression, visitedExpression);
   }

   private static void ValidateVisitedExpressions(Expression<Func<MyObject, bool>> expression, Expression<Func<MyObject, bool>> visitedExpression)
   {
      visitedExpression.Should()
                       .NotBeNull().And
                       .NotBe(expression);

      var rightMemberAccess = visitedExpression.Body.As<BinaryExpression>()   // o.MyProperty == o.MyOtherProperty
                                               .Right.As<MemberExpression>(); // o.MyOtherProperty

      rightMemberAccess.Should().NotBeNull();

      rightMemberAccess.Member.Should().Be(_myOtherProperty);             // MyOtherProperty
      rightMemberAccess.Expression.Should().Be(expression.Parameters[0]); // o
   }

   private class MyObject
   {
      // ReSharper disable once UnusedAutoPropertyAccessor.Local
      public string? MyProperty { get; set; }
      public string? MyOtherProperty { get; set; }

      public static Expression<Func<MyObject, string?>> GetExpressionFromStaticMethod()
      {
         return o => o.MyOtherProperty;
      }

      public Expression<Func<MyObject, string?>> GetExpressionFromInstanceMethod()
      {
         return o => o.MyOtherProperty;
      }
   }
}