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
      /// Creates a temp table using entity/query type '<typeparamref name="T"/>'.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of the entity/query type.</typeparam>
      /// <returns>Table name.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      Task<string> CreateTempTableAsync<T>([NotNull] DbContext ctx, bool makeTableNameUnique = false, CancellationToken cancellationToken = default)
         where T : class;

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="type">Entity/query type.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Table name.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="ctx"/> is <c>null</c>
      /// - or
      /// <paramref name="type"/> is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by the <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      Task<string> CreateTempTableAsync([NotNull] DbContext ctx, [NotNull] Type type, bool makeTableNameUnique = false, CancellationToken cancellationToken = default);

      /// <summary>
      /// Creates a primary key in a temp table with provided <paramref name="tableName"/>.
      /// 
      /// <remarks>
      /// If the type <typeparamref name="T"/> is a query type then all columns are part of the primary key
      /// because a query type has no "Primary Key" by definition.
      /// </remarks>
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="tableName">Table name to create the primary key in.</param>
      /// <param name="checkForExistence">If <c>true</c> then the primary key is not going to be created if it exists already.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Entity/query type.</typeparam>
      [NotNull]
      Task CreatePrimaryKeyAsync<T>([NotNull] DbContext ctx, [NotNull] string tableName, bool checkForExistence = false, CancellationToken cancellationToken = default);
   }
}
