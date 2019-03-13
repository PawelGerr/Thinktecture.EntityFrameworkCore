using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   public static class DbContextExtensions
   {
      private static readonly NumberToBytesConverter<long> _timestampConverter = new NumberToBytesConverter<long>();

      public static async Task<long> GetMinActiveRowVersionAsync([NotNull] this DbContext ctx, CancellationToken cancellationToken)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

         using (var command = ctx.Database.GetDbConnection().CreateCommand())
         {
            await ctx.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            command.CommandText = "SELECT MIN_ACTIVE_ROWVERSION();";
            var bytes = (byte[])await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            return (long)_timestampConverter.ConvertFromProvider(bytes);
         }
      }
   }
}
