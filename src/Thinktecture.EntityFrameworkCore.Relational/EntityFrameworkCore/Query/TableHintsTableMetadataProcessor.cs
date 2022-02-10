using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

internal class TableHintsTableMetadataProcessor : ITableMetadataProcessor
{
   public static readonly TableHintsTableMetadataProcessor Instance = new();

   public int Order => 100;

   public TableExpressionBase ProcessMetadata(TableExpressionBase expression, TableWithMetadata tableWithMetadata)
   {
      if (tableWithMetadata.Metadata.TryGetValue(nameof(TableWithHintsExpression), out var value) && value is IReadOnlyList<ITableHint> tableHints)
         return new TableWithHintsExpression(expression, tableHints);

      return expression;
   }
}
