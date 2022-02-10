using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Adds table hints.
/// </summary>
public class RelationalOptimizingVisitor : DefaultSqlExpressionVisitor
{
   private readonly IReadOnlyList<ITableMetadataProcessor> _metadataProcessors;

   /// <summary>
   /// Initializes new instance of <see cref="RelationalOptimizingVisitor"/>.
   /// </summary>
   public RelationalOptimizingVisitor(RelationalDbContextOptionsExtensionOptions options)
   {
      var processorsWithSameOrder = options.MetadataProcessors
                                           .GroupBy(p => p.Order)
                                           .Where(g => g.Count() > 1)
                                           .SelectMany(g => g)
                                           .ToList();

      if (processorsWithSameOrder.Count > 0)
         throw new ArgumentException($"Multiple MetadataProcessors with the same order: {String.Join(", ", processorsWithSameOrder.Select(p => $"{{ Type='{p.GetType().Name}', Order={p.Order} }}"))}");

      _metadataProcessors = options.MetadataProcessors.OrderBy(p => p.Order).ToList();
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
   protected override Expression VisitTable(TableExpression tableExpression)
   {
      var visitedTable = base.VisitTable(tableExpression);

      if (visitedTable is TableExpression { Table: TableWithMetadata tableWithMetadata } visitedTableExpression)
         visitedTable = ProcessMetadata(visitedTableExpression, tableWithMetadata);

      return visitedTable;
   }

   private Expression ProcessMetadata(TableExpressionBase tableExpression, TableWithMetadata tableWithMetadata)
   {
      for (var i = 0; i < _metadataProcessors.Count; i++)
      {
         tableExpression = _metadataProcessors[i].ProcessMetadata(tableExpression, tableWithMetadata);
      }

      return tableExpression;
   }
}
