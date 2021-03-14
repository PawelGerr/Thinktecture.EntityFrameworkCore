using System;
using System.Linq.Expressions;

namespace Thinktecture.Internal
{
   /// <summary>
   /// For internal use only.
   /// </summary>
   public class NonEvaluatableConstantExpression : Expression
   {
      /// <summary>
      /// Value.
      /// </summary>
      public object? Value { get; }

      /// <inheritdoc />
      public override Type Type => Value is null ? typeof(object) : Value.GetType();

      /// <inheritdoc />
      public override ExpressionType NodeType => ExpressionType.Extension;

      /// <summary>
      /// Initializes new instance of <see cref="NonEvaluatableConstantExpression"/>
      /// </summary>
      /// <param name="value"></param>
      public NonEvaluatableConstantExpression(object? value)
      {
         Value = value;
      }

      /// <inheritdoc />
      protected override Expression Accept(ExpressionVisitor visitor)
      {
         return this;
      }
   }
}
