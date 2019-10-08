using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class SqliteDbContextExtensions
   {
      /// <summary>
      /// Copies <paramref name="entities"/> into a table.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to insert.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity type.</typeparam>
      /// <exception cref="ArgumentNullException"> <paramref name="ctx"/> or <paramref name="entities"/> is <c>null</c>.</exception>
      public static Task BulkInsertAsync<T>(this DbContext ctx,
                                            IEnumerable<T> entities,
                                            SqliteBulkInsertOptions? options,
                                            CancellationToken cancellationToken = default)
         where T : class
      {
         return ctx.BulkInsertAsync(entities, (IBulkInsertOptions?)options, cancellationToken);
      }
   }
}
