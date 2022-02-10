using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// For internal use only.
/// </summary>
public class TempTableTableMetadataProcessor : ITableMetadataProcessor
{
   /// <summary>
   /// Singleton.
   /// </summary>
   public static readonly TempTableTableMetadataProcessor Instance = new();

   /// <summary>
   /// For internal use only.
   /// </summary>
   public int Order => 10;

   /// <summary>
   /// For internal use only.
   /// </summary>
   public TableExpressionBase ProcessMetadata(TableExpressionBase expression, TableWithMetadata tableWithMetadata)
   {
      if (tableWithMetadata.Metadata.TryGetValue(nameof(TempTableExpression), out var value) && value is string tempTableName)
         return new TempTableExpression(tempTableName, expression.Alias);

      return expression;
   }
}
