using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Thinktecture.Linq.Expressions
{
   /// <summary>
   /// Replaces one expression with another.
   /// </summary>
   public class ExpressionReplacingVisitor : ExpressionVisitor
   {
      private readonly Expression _oldExpression;
      private readonly Expression _newExpression;

      /// <summary>
      /// Initializes <see cref="ExpressionReplacingVisitor"/>.
      /// </summary>
      /// <param name="oldExpression">Expression to replace.</param>
      /// <param name="newExpression">Expression to replace with.</param>
      public ExpressionReplacingVisitor([NotNull] Expression oldExpression, [NotNull] Expression newExpression)
      {
         _oldExpression = oldExpression ?? throw new ArgumentNullException(nameof(oldExpression));
         _newExpression = newExpression ?? throw new ArgumentNullException(nameof(newExpression));
      }

      /// <inheritdoc />
      public override Expression Visit(Expression node)
      {
         return node == _oldExpression ? _newExpression : base.Visit(node);
      }
   }
}
