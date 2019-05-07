using System;
using System.Linq.Expressions;
using FluentAssertions;
using Xunit;

namespace Thinktecture.Linq.Expressions.ExpressionReplacingVisitorTests
{
   public class Visit
   {
      [Fact]
      public void Should_not_change_expression_if_expression_not_exists()
      {
         Expression<Func<int, bool>> expression = s => s == 1;
         var visitor = new ExpressionReplacingVisitor(Expression.Constant(1), Expression.Constant(2));

         var visitedExpression = visitor.Visit(expression);

         visitedExpression.Should().Be(expression);
      }

      [Fact]
      public void Should_replace_found_expression()
      {
         Expression<Func<int, bool>> expression = s => s == 1;
         var oldExpression = expression.As<LambdaExpression>().Body.As<BinaryExpression>().Right;
         var newExpression = Expression.Constant(2);
         var visitor = new ExpressionReplacingVisitor(oldExpression, newExpression);

         var visitedExpression = visitor.Visit(expression);

         visitedExpression.As<LambdaExpression>().Body.As<BinaryExpression>().Right.Should().Be(newExpression);
      }
   }
}
