using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// MySQL-specific extension methods for <see cref="DbContext"/>.
/// </summary>
public static class MySqlBulkOperationsDbContextExtensions
{
   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/> with <c>TRUNCATE TABLE</c>.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="throwOnTransaction">
   /// If <c>true</c>, the method throws when a transaction is active.
   /// If <c>false</c>, <c>TRUNCATE TABLE</c> is executed inside the active transaction, and MySQL commits this transaction implicitly.
   /// </param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   /// <remarks>
   /// See <see cref="MySqlBulkOperationExecutor.TruncateTableAsync{T}(bool, CancellationToken)"/> for the detected transactions and the server errors.
   /// The PostgreSQL package has the method <c>TruncateTableAsync(bool cascade)</c> with the same shape in the same namespace.
   /// Therefore a project that references both packages passes the argument by name (<c>throwOnTransaction: false</c>), otherwise the call fails with error CS0121.
   /// </remarks>
   /// <returns>A task that completes when the table is truncated.</returns>
   /// <exception cref="InvalidOperationException">The parameter <paramref name="throwOnTransaction"/> is <c>true</c> and a transaction is active.</exception>
   /// <exception cref="NotSupportedException">The table has a schema and the schema behavior of the provider is <c>Throw</c>.</exception>
   public static Task TruncateTableAsync<T>(
      this DbContext ctx,
      bool throwOnTransaction,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return ctx.GetService<MySqlBulkOperationExecutor>()
                .TruncateTableAsync<T>(throwOnTransaction, cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <paramref name="type"/> with <c>TRUNCATE TABLE</c>.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="type">Type of the entity to truncate.</param>
   /// <param name="throwOnTransaction">
   /// If <c>true</c>, the method throws when a transaction is active.
   /// If <c>false</c>, <c>TRUNCATE TABLE</c> is executed inside the active transaction, and MySQL commits this transaction implicitly.
   /// </param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <remarks>
   /// See <see cref="MySqlBulkOperationExecutor.TruncateTableAsync(Type, bool, CancellationToken)"/> for the detected transactions and the server errors.
   /// The PostgreSQL package has the method <c>TruncateTableAsync(Type type, bool cascade)</c> with the same shape in the same namespace.
   /// Therefore a project that references both packages passes the argument by name (<c>throwOnTransaction: false</c>), otherwise the call fails with error CS0121.
   /// </remarks>
   /// <returns>A task that completes when the table is truncated.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">The parameter <paramref name="throwOnTransaction"/> is <c>true</c> and a transaction is active.</exception>
   /// <exception cref="NotSupportedException">The table has a schema and the schema behavior of the provider is <c>Throw</c>.</exception>
   public static Task TruncateTableAsync(
      this DbContext ctx,
      Type type,
      bool throwOnTransaction,
      CancellationToken cancellationToken = default)
   {
      return ctx.GetService<MySqlBulkOperationExecutor>()
                .TruncateTableAsync(type, throwOnTransaction, cancellationToken);
   }
}
