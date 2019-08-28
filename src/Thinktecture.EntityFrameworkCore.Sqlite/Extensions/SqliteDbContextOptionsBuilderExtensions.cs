using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="SqliteDbContextOptionsBuilder"/>.
   /// </summary>
   public static class SqliteDbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Adds support for bulk operations.
      /// </summary>
      /// <param name="sqliteOptionsBuilder">SQLite options builder.</param>
      /// <param name="addBulkOperationSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqliteOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqliteDbContextOptionsBuilder AddBulkOperationSupport([NotNull] this SqliteDbContextOptionsBuilder sqliteOptionsBuilder,
                                                                          bool addBulkOperationSupport = true)
      {
         return AddOrUpdateExtensions(sqliteOptionsBuilder, extension => extension.AddBulkOperationSupport = addBulkOperationSupport);
      }

      [NotNull]
      private static SqliteDbContextOptionsBuilder AddOrUpdateExtensions([NotNull] this SqliteDbContextOptionsBuilder sqliteOptionsBuilder,
                                                                         [NotNull] Action<SqliteDbContextOptionsExtension> callback)
      {
         var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)sqliteOptionsBuilder;
         infrastructure.OptionsBuilder.AddOrUpdateExtension(callback);

         return sqliteOptionsBuilder;
      }
   }
}
