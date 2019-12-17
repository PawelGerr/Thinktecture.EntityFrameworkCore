using System;
using System.Collections.Generic;
using System.Linq;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a query pointing to a temp table.
   /// </summary>
   /// <typeparam name="T">Type of the query item.</typeparam>
   public sealed class TempTableQuery<T> : ITempTableQuery<T>
   {
      /// <summary>
      /// The query itself.
      /// </summary>
      public IQueryable<T> Query { get; }

      private ITempTableReference? _tempTableReference;

      /// <summary>
      /// Initializes new instance of <see cref="TempTableQuery{T}"/>.
      /// </summary>
      /// <param name="query">Query.</param>
      /// <param name="tempTableReference">Reference to a temp table.</param>
      public TempTableQuery(IQueryable<T> query, ITempTableReference tempTableReference)
      {
         Query = query ?? throw new ArgumentNullException(nameof(query));
         _tempTableReference = tempTableReference ?? throw new ArgumentNullException(nameof(tempTableReference));
      }

      /// <inheritdoc />
      public void Dispose()
      {
         _tempTableReference?.Dispose();
         _tempTableReference = null;
      }
   }
}
