using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates temp tables.
   /// </summary>
   public interface ITempTableCreator
   {
      /// <summary>
      /// Creates a temp table using custom type '<typeparamref name="T"/>'.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of custom temp table.</typeparam>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      Task<string> CreateTempTableAsync<T>([NotNull] DbContext ctx, bool makeTableNameUnique = false, CancellationToken cancellationToken = default)
         where T : class;

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="type">Type of the entity.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="ctx"/> is <c>null</c>
      /// - or
      /// <paramref name="type"/> is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      Task<string> CreateTempTableAsync([NotNull] DbContext ctx, [NotNull] Type type, bool makeTableNameUnique = false, CancellationToken cancellationToken = default);
   }
}
