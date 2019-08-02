using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Represents a query pointing to a temp table.
   /// </summary>
   /// <typeparam name="T">Type of the query item.</typeparam>
#pragma warning disable  CA1710
   public sealed class TempTableQuery<T> : ITempTableQuery<T>
#pragma warning restore CA1710
   {
      /// <summary>
      /// The query itself.
      /// </summary>
      public IQueryable<T> Query { get; }

      private readonly ITempTableReference _tempTableReference;

      /// <summary>
      /// Initializes new instance of <see cref="TempTableQuery{T}"/>.
      /// </summary>
      /// <param name="query">Query.</param>
      /// <param name="tempTableReference">Reference to a temp table.</param>
      public TempTableQuery([NotNull] IQueryable<T> query, [NotNull] ITempTableReference tempTableReference)
      {
         Query = query ?? throw new ArgumentNullException(nameof(query));
         _tempTableReference = tempTableReference ?? throw new ArgumentNullException(nameof(tempTableReference));
      }

      /// <inheritdoc />
      public void Dispose()
      {
         _tempTableReference.Dispose();
      }
   }
}
