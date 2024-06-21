using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// Represents PARTITION BY.
/// </summary>
public class WindowFunctionPartitionByExpression : SqlExpression, INotNullableSqlExpression
{
   /// <summary>
   /// Partition by expressions.
   /// </summary>
   public IReadOnlyList<SqlExpression> PartitionBy { get; }

   /// <inheritdoc />
   public WindowFunctionPartitionByExpression(IReadOnlyList<SqlExpression> partitionBy)
      : base(typeof(WindowFunctionPartitionByClause), RelationalTypeMapping.NullMapping)
   {
      PartitionBy = partitionBy ?? throw new ArgumentNullException(nameof(partitionBy));
   }

   /// <inheritdoc />
   protected override Expression Accept(ExpressionVisitor visitor)
   {
      if (visitor is QuerySqlGenerator)
         throw new NotSupportedException("The window function contains some expressions not supported by the Entity Framework.");

      return base.Accept(visitor);
   }

   /// <inheritdoc />
   protected override Expression VisitChildren(ExpressionVisitor visitor)
   {
      var visited = visitor.VisitExpressions(PartitionBy);

      return ReferenceEquals(visited, PartitionBy) ? this : new WindowFunctionPartitionByExpression(visited);
   }

   /// <inheritdoc />
   protected override void Print(ExpressionPrinter expressionPrinter)
   {
      ArgumentNullException.ThrowIfNull(expressionPrinter);

      expressionPrinter.VisitCollection(PartitionBy);
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj != null && (ReferenceEquals(this, obj) || Equals(obj as WindowFunctionPartitionByExpression));
   }

   private bool Equals(WindowFunctionPartitionByExpression? expression)
   {
      return base.Equals(expression) && PartitionBy.SequenceEqual(expression.PartitionBy);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hash = new HashCode();
      hash.Add(base.GetHashCode());

      for (var i = 0; i < PartitionBy.Count; i++)
      {
         hash.Add(PartitionBy[i]);
      }

      return hash.ToHashCode();
   }
}
