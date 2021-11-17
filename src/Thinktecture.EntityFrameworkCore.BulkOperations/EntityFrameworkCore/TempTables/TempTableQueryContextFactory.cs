using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates <see cref="TempTableQueryContext"/> and keeps track of number of contexts being used in a query.
   /// </summary>
   public class TempTableQueryContextFactory
   {
      private const string _PREFIX = "Thinktecture:TempTableQueryContext:";

      private int _counter;

      /// <summary>
      /// Creates new <see cref="TempTableQueryContext"/>.
      /// </summary>
      /// <param name="table">Table to create the context for.</param>
      /// <param name="tempTableName">Name of the temp table.</param>
      /// <returns>A new instance of see <see cref="TempTableQueryContext"/>.</returns>
      public TempTableQueryContext Create(TableExpression table, string tempTableName)
      {
         return new(table, tempTableName, $"{_PREFIX}:{tempTableName}:{++_counter}");
      }
   }
}
