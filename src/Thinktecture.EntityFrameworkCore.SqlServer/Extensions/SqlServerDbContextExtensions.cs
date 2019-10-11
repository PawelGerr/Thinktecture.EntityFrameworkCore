using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class SqlServerDbContextExtensions
   {
      private static readonly NumberToBytesConverter<long> _rowVersionConverter = new NumberToBytesConverter<long>();

      /// <summary>
      /// Fetches <c>MIN_ACTIVE_ROWVERSION</c> from SQL Server.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>The result of <c>MIN_ACTIVE_ROWVERSION</c> call.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task<long> GetMinActiveRowVersionAsync(this DbContext ctx, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         await using var command = ctx.Database.GetDbConnection().CreateCommand();

         command.Transaction = ctx.Database.CurrentTransaction?.GetDbTransaction();
         command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";

         await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

         try
         {
            var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return (long)_rowVersionConverter.ConvertFromProvider(bytes);
         }
         finally
         {
            ctx.Database.CloseConnection();
         }
      }
   }
}
