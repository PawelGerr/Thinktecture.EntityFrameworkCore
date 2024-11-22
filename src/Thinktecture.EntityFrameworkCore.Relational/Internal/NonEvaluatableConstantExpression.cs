using System.Linq.Expressions;

namespace Thinktecture.Internal;

/// <summary>
/// For internal use only.
/// </summary>
public abstract class NonEvaluatableConstantExpression<T> : Expression, IEquatable<T>
   where T : notnull
{
   /// <summary>
   /// Value.
   /// </summary>
   public T Value { get; }

   /// <inheritdoc />
   public override Type Type { get; }

   /// <inheritdoc />
   public override ExpressionType NodeType => ExpressionType.Extension;

   /// <summary>
   /// Initializes new instance of <see cref="NonEvaluatableConstantExpression{T}"/>
   /// </summary>
   /// <param name="value"></param>
   public NonEvaluatableConstantExpression(T value)
   {
      Value = value;
      Type = Value.GetType();
   }

   /// <inheritdoc />
   protected override Expression Accept(ExpressionVisitor visitor)
   {
      return this;
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj != null
             && (ReferenceEquals(this, obj)
                 || (obj is NonEvaluatableConstantExpression<T> other && Equals(other.Value)));
   }

   /// <summary>
   /// Compares <see cref="Value"/> with <paramref name="otherValue"/>.
   /// </summary>
   /// <param name="otherValue">Other value.</param>
   /// <returns>Return</returns>
   public abstract bool Equals(T? otherValue);

   /// <inheritdoc />
   public abstract override int GetHashCode();
}
