using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Adds table hints.
/// </summary>
public class RelationalOptimizingVisitor : DefaultSqlExpressionVisitor
{
   /// <summary>
   /// Runtime parameters.
   /// </summary>
   protected IReadOnlyDictionary<string, object?> ParametersValues { get; }

   /// <summary>
   /// Table hints.
   /// </summary>
   protected IReadOnlyList<TableHintContext> TableHintContexts { get; }

   /// <summary>
   /// Initializes new instance of <see cref="RelationalOptimizingVisitor"/>.
   /// </summary>
   /// <param name="parametersValues">Runtime parameters.</param>
   /// <param name="tableHintContexts">Table hints.</param>
   public RelationalOptimizingVisitor(
      IReadOnlyDictionary<string, object?> parametersValues,
      IReadOnlyList<TableHintContext> tableHintContexts)
   {
      ParametersValues = parametersValues;
      TableHintContexts = tableHintContexts ?? throw new ArgumentNullException(nameof(tableHintContexts));
   }

   /// <summary>
   /// Replaces tables with temp tables.
   /// </summary>
   /// <param name="selectExpression">Select expression to visit.</param>
   /// <returns>Visited select expression.</returns>
   public SelectExpression Process(SelectExpression selectExpression)
   {
      return (SelectExpression)VisitSelect(selectExpression);
   }

   /// <inheritdoc />
   protected override TableExpressionBase VisitTableExpressionBase(TableExpressionBase table)
   {
      var newTable = base.VisitTableExpressionBase(table);

      return AddTableHints(table, newTable);
   }

   private TableExpressionBase AddTableHints(TableExpressionBase oldTable, TableExpressionBase newTable)
   {
      var tableHintCtx = TableHintContexts.SingleOrDefault(c => c.Table.Equals(oldTable));

      if (tableHintCtx is not null)
         return new TableWithHintsExpression(newTable, tableHintCtx.TableHints);

      return newTable;
   }
}
