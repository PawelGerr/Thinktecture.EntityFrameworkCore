using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// PostgreSQL-specific extension methods for <see cref="DbContext"/>.
/// </summary>
public static class NpgsqlBulkOperationsDbContextExtensions
{
   /// <summary>
   /// Truncates the table of the entity of type <typeparamref name="T"/> with optional CASCADE.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="cascade">If <c>true</c>, CASCADE is appended to the TRUNCATE statement.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <typeparam name="T">Type of the entity to truncate.</typeparam>
   public static Task TruncateTableAsync<T>(
      this DbContext ctx,
      bool cascade,
      CancellationToken cancellationToken = default)
      where T : class
   {
      return ctx.GetService<INpgsqlTruncateTableExecutor>()
                .TruncateTableAsync<T>(cascade, cancellationToken);
   }

   /// <summary>
   /// Truncates the table of the entity of type <paramref name="type"/> with optional CASCADE.
   /// </summary>
   /// <param name="ctx">Database context.</param>
   /// <param name="type">Type of the entity to truncate.</param>
   /// <param name="cascade">If <c>true</c>, CASCADE is appended to the TRUNCATE statement.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   public static Task TruncateTableAsync(
      this DbContext ctx,
      Type type,
      bool cascade,
      CancellationToken cancellationToken = default)
   {
      return ctx.GetService<INpgsqlTruncateTableExecutor>()
                .TruncateTableAsync(type, cascade, cancellationToken);
   }
}
