using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.EntityFrameworkCore.ValueConversion;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class DbContextExtensions
   {
      private static readonly RowVersionValueConverter _rowVersionConverter;
      private static readonly ConcurrentDictionary<Type, string> _tempTableCreationSql;

      static DbContextExtensions()
      {
         _rowVersionConverter = new RowVersionValueConverter();
         _tempTableCreationSql = new ConcurrentDictionary<Type, string>();
      }

      /// <summary>
      /// Fetches MIN_ACTIVE_ROWVERSION from SQL Server.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>The result of MIN_ACTIVE_ROWVERSION call.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      public static async Task<ulong> GetMinActiveRowVersionAsync([NotNull] this DbContext ctx, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         using (var command = ctx.Database.GetDbConnection().CreateCommand())
         {
            command.Transaction = ctx.Database.CurrentTransaction?.GetDbTransaction();

            await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";
            var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return (ulong)_rowVersionConverter.ConvertFromProvider(bytes);
         }
      }
   }
}
