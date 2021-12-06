using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

/// <summary>
/// An expression that represents table hints.
/// </summary>
public sealed class TableWithHintsExpression : TableExpressionBase
{
   /// <summary>
   /// Table to apply hints to.
   /// </summary>
   public TableExpressionBase Table { get; }

   /// <summary>
   /// Table hints.
   /// </summary>
   public IReadOnlyList<ITableHint> TableHints { get; }

   /// <summary>
   /// Initializes new instance of <see cref="TableWithHintsExpression"/>.
   /// </summary>
   /// <param name="table">Table to apply table hints to.</param>
   /// <param name="tableHints">Table hints.</param>
   public TableWithHintsExpression(TableExpressionBase table, IReadOnlyList<ITableHint> tableHints)
      : base(table.Alias)
   {
      Table = table;
      TableHints = tableHints;
   }

   /// <inheritdoc />
   protected override void Print(ExpressionPrinter expressionPrinter)
   {
      expressionPrinter.Visit(Table);

      expressionPrinter.Append("WITH (");

      for (var i = 0; i < TableHints.Count; i++)
      {
         if (i != 0)
            expressionPrinter.Append(", ");

         expressionPrinter.Append(TableHints[i].ToString()!);
      }

      expressionPrinter.Append(")");
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return ReferenceEquals(this, obj) || Equals(obj as TableWithHintsExpression);
   }

   private bool Equals(TableWithHintsExpression? tableWithHintsExpression)
   {
      return base.Equals(tableWithHintsExpression)
             && Table.Equals(tableWithHintsExpression.Table)
             && Equals(TableHints, tableWithHintsExpression.TableHints);
   }

   private static bool Equals(IReadOnlyList<ITableHint> hints, IReadOnlyList<ITableHint> otherHints)
   {
      if (hints.Count != otherHints.Count)
         return false;

      foreach (var tableHint in hints)
      {
         if (!otherHints.Contains(tableHint))
            return false;
      }

      return true;
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hashCode = new HashCode();
      hashCode.Add(base.GetHashCode());
      hashCode.Add(Table);

      for (var i = 0; i < TableHints.Count; i++)
      {
         hashCode.Add(TableHints[i]);
      }

      return hashCode.ToHashCode();
   }
}
