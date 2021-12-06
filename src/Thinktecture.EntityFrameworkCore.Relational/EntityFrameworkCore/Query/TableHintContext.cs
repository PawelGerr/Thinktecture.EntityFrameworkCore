using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// This is an internal API.
/// </summary>
public class TableHintContext
{
   private const string _PREFIX = "Thinktecture:TableHintContext:";

   /// <summary>
   /// Table expression that gets the query hints.
   /// </summary>
   public TableExpressionBase Table { get; }

   /// <summary>
   /// Table hints.
   /// </summary>
   public IReadOnlyList<ITableHint> TableHints { get; }

   /// <summary>
   /// Runtime parameter name.
   /// </summary>
   public string ParameterName { get; }

   internal TableHintContext(TableExpressionBase table, IReadOnlyList<ITableHint> tableHints, int index)
   {
      Table = table;
      TableHints = tableHints;
      ParameterName = $"{_PREFIX}{index}";
   }

   /// <summary>
   /// This is an internal API.
   /// </summary>
   public static bool TryGetTableHintContext(
      IReadOnlyDictionary<string, object?> parametersValues,
      [NotNullWhen(true)] out IReadOnlyList<TableHintContext>? tableHintContexts)
   {
      List<TableHintContext>? ctxs = null;

      foreach (var (_, value) in parametersValues)
      {
         if (value is not TableHintContext ctx)
            continue;

         ctxs ??= new List<TableHintContext>();
         ctxs.Add(ctx);
      }

      tableHintContexts = ctxs;
      return tableHintContexts is not null;
   }
}
