using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query;

/// <summary>
/// Creates <see cref="TableHintContext"/> and keeps track of number of contexts being used in a query.
/// </summary>
public class TableHintContextFactory
{
   private int _counter;

   /// <summary>
   /// Creates new <see cref="TableHintContext"/>.
   /// </summary>
   /// <param name="table">Table which gets the table hints.</param>
   /// <param name="hints">Table hints.</param>
   /// <returns>A new instance of see <see cref="TableHintContext"/>.</returns>
   public TableHintContext Create(TableExpressionBase table, IReadOnlyList<ITableHint> hints)
   {
      return new(table, hints, ++_counter);
   }
}
