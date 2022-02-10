using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// For internal use only.
/// </summary>
public interface ITableMetadataProcessor
{
   /// <summary>
   /// For internal use only.
   /// </summary>
   int Order { get; }

   /// <summary>
   /// For internal use only.
   /// </summary>
   TableExpressionBase ProcessMetadata(TableExpressionBase expression, TableWithMetadata tableWithMetadata);
}
