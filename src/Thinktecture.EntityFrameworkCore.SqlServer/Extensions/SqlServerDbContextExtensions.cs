using System.Text;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="DbContext"/>.
/// </summary>
public static class SqlServerDbContextExtensions
{
   private static readonly NumberToBytesConverter<long> _rowVersionConverter = new();

   /// <summary>
   /// Fetches <c>MIN_ACTIVE_ROWVERSION</c> from SQL Server.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The result of <c>MIN_ACTIVE_ROWVERSION</c> call.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
   public static Task<long> GetMinActiveRowVersionAsync(this DbContext ctx, CancellationToken cancellationToken = default)
   {
      return GetRowVersionAsync(ctx, "MIN_ACTIVE_ROWVERSION()", cancellationToken);
   }

   /// <summary>
   /// Fetches <c>@@DBTS</c> from SQL Server.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The result of <c>@@DBTS</c> call.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
   public static Task<long> GetLastUsedRowVersionAsync(this DbContext ctx, CancellationToken cancellationToken = default)
   {
      return GetRowVersionAsync(ctx, "@@DBTS", cancellationToken);
   }

   private static async Task<long> GetRowVersionAsync(DbContext ctx, string dbFunction, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(ctx);

      await using var command = ctx.Database.GetDbConnection().CreateCommand();

      command.Transaction = ctx.Database.CurrentTransaction?.GetDbTransaction();
      command.CommandText = $"SELECT {dbFunction};";

      await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

      try
      {
         var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
         var bytes = (byte[])result!;

         return (long)_rowVersionConverter.ConvertFromProvider(bytes)!;
      }
      finally
      {
         await ctx.Database.CloseConnectionAsync().ConfigureAwait(false);
      }
   }
}
