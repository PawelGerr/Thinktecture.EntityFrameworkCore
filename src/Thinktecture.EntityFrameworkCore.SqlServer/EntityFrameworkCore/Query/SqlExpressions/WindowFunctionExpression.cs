using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// Window function expression.
/// </summary>
public class WindowFunctionExpression : SqlExpression
{
   /// <summary>
   /// Creates a new instance of the <see cref="WindowFunctionExpression" /> class.
   /// </summary>
   /// <param name="aggregateFunction">Aggregate function.</param>
   /// <param name="partitions">A list expressions to partition by.</param>
   /// <param name="orderings">A list of ordering expressions to order by.</param>
   public WindowFunctionExpression(
      SqlExpression aggregateFunction,
      IReadOnlyList<SqlExpression>? partitions,
      IReadOnlyList<OrderingExpression>? orderings)
      : base(aggregateFunction.Type, aggregateFunction.TypeMapping)
   {
      Partitions = partitions ?? Array.Empty<SqlExpression>();
      AggregateFunction = aggregateFunction;
      Orderings = orderings ?? Array.Empty<OrderingExpression>();
   }

   /// <summary>
   /// Aggregate function.
   /// </summary>
   public SqlExpression AggregateFunction { get; }

   /// <summary>
   ///     The list of expressions used in partitioning.
   /// </summary>
   public virtual IReadOnlyList<SqlExpression> Partitions { get; }

   /// <summary>
   ///     The list of ordering expressions used to order inside the given partition.
   /// </summary>
   public virtual IReadOnlyList<OrderingExpression> Orderings { get; }

   /// <inheritdoc />
   protected override Expression VisitChildren(ExpressionVisitor visitor)
   {
      var partitions = new List<SqlExpression>();

      var visitedAggregateFunction = (SqlExpression)visitor.Visit(AggregateFunction);

      var changed = AggregateFunction != visitedAggregateFunction;

      foreach (var partition in Partitions)
      {
         var newPartition = (SqlExpression)visitor.Visit(partition);
         changed |= newPartition != partition;
         partitions.Add(newPartition);
      }

      var orderings = new List<OrderingExpression>();

      foreach (var ordering in Orderings)
      {
         var newOrdering = (OrderingExpression)visitor.Visit(ordering);
         changed |= newOrdering != ordering;
         orderings.Add(newOrdering);
      }

      return changed
                ? new WindowFunctionExpression(visitedAggregateFunction, partitions, orderings)
                : this;
   }

   /// <summary>
   /// Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.
   /// </summary>
   /// <param name="aggregateFunction">Aggregate function.</param>
   /// <param name="partitions">The <see cref="Partitions" /> property of the result.</param>
   /// <param name="orderings">The <see cref="Orderings" /> property of the result.</param>
   /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
   public virtual WindowFunctionExpression Update(
      SqlExpression aggregateFunction,
      IReadOnlyList<SqlExpression>? partitions,
      IReadOnlyList<OrderingExpression>? orderings)
   {
      return AggregateFunction == aggregateFunction
             && ((Partitions == null && partitions == null) || (Partitions != null && partitions != null && Partitions.SequenceEqual(partitions)))
             && ((Orderings == null && orderings == null) || (Orderings != null && orderings != null && Orderings.SequenceEqual(orderings)))
                ? this
                : new WindowFunctionExpression(aggregateFunction, partitions, orderings);
   }

   /// <inheritdoc />
   protected override void Print(ExpressionPrinter expressionPrinter)
   {
      expressionPrinter.Visit(AggregateFunction);

      expressionPrinter.Append(" OVER(");

      if (Partitions.Count != 0)
      {
         expressionPrinter.Append("PARTITION BY ");
         expressionPrinter.VisitCollection(Partitions);
         expressionPrinter.Append(" ");
      }

      if (Partitions.Count != 0)
      {
         expressionPrinter.Append("ORDER BY ");
         expressionPrinter.VisitCollection(Orderings);
      }

      expressionPrinter.Append(")");
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj != null
             && (ReferenceEquals(this, obj) || obj is WindowFunctionExpression windowFunctionExpression && Equals(windowFunctionExpression));
   }

   private bool Equals(WindowFunctionExpression windowFunctionExpression)
   {
      return base.Equals(windowFunctionExpression)
             && AggregateFunction.Equals(windowFunctionExpression.AggregateFunction)
             && (Partitions == null ? windowFunctionExpression.Partitions == null : Partitions.SequenceEqual(windowFunctionExpression.Partitions))
             && (Orderings == null ? windowFunctionExpression.Orderings == null : Orderings.SequenceEqual(windowFunctionExpression.Orderings));
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hash = new HashCode();
      hash.Add(base.GetHashCode());
      hash.Add(AggregateFunction.GetHashCode());

      foreach (var partition in Partitions)
      {
         hash.Add(partition);
      }

      foreach (var ordering in Orderings)
      {
         hash.Add(ordering);
      }

      return hash.ToHashCode();
   }
}
