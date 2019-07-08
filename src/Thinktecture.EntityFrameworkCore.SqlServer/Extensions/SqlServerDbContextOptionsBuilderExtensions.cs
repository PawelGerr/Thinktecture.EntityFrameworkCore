using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="SqlServerDbContextOptionsBuilder"/>.
   /// </summary>
   public static class SqlServerDbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Adds support for "RowNumber".
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqlServerDbContextOptionsBuilder AddRowNumberSupport([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder)
      {
         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.AddRowNumberSupport = true);
      }

      /// <summary>
      /// Adds support for temp tables.
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqlServerDbContextOptionsBuilder AddTempTableSupport([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder)
      {
         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.AddTempTableSupport = true);
      }

      [NotNull]
      private static SqlServerDbContextOptionsBuilder AddOrUpdateExtensions([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                            [NotNull] Action<SqlServerDbContextOptionsExtension> callback)
      {
         if (sqlServerOptionsBuilder == null)
            throw new ArgumentNullException(nameof(sqlServerOptionsBuilder));
         if (callback == null)
            throw new ArgumentNullException(nameof(callback));

         var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)sqlServerOptionsBuilder;
         var extension = infrastructure.OptionsBuilder.Options.FindExtension<SqlServerDbContextOptionsExtension>() ?? new SqlServerDbContextOptionsExtension();

         callback(extension);

         var builder = (IDbContextOptionsBuilderInfrastructure)infrastructure.OptionsBuilder;
         builder.AddOrUpdateExtension(extension);

         return sqlServerOptionsBuilder;
      }
   }
}
