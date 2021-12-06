using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// Marker-expression that leads to generation of a DELETE statement.
/// </summary>
public class DeleteExpression : SqlExpression, INotNullableSqlExpression
{
   /// <summary>
   /// Table to delete the records from.
   /// </summary>
   public TableExpressionBase Table { get; }

   /// <summary>
   /// Initializes new instance of <see cref="DeleteExpression"/>.
   /// </summary>
   /// <param name="table">Table to delete the records from.</param>
   /// <param name="typeMapping">Type mapping.</param>
   public DeleteExpression(
      TableExpressionBase table,
      RelationalTypeMapping typeMapping)
      : base(typeof(int), typeMapping)
   {
      Table = table ?? throw new ArgumentNullException(nameof(table));
   }

   /// <inheritdoc />
   protected override Expression Accept(ExpressionVisitor visitor)
   {
      return this;
   }

   /// <inheritdoc />
   protected override Expression VisitChildren(ExpressionVisitor visitor)
   {
      return this;
   }

   /// <inheritdoc />
   protected override void Print(ExpressionPrinter expressionPrinter)
   {
      expressionPrinter.Append("1;").AppendLine()
                       .Append("DELETE ");

      if (Table.Alias is not null)
         expressionPrinter.Append(Table.Alias).Append(" ");
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj != null && (ReferenceEquals(this, obj) || Equals(obj as DeleteExpression));
   }

   private bool Equals(DeleteExpression? expression)
   {
      return base.Equals(expression) && Table.Equals(expression.Table);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      return HashCode.Combine(base.GetHashCode(), Table);
   }
}
