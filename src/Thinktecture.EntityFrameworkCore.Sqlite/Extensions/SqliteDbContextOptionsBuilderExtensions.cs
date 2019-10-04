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
      /// Adds custom factory required for translation of custom methods like <see cref="QueryableExtensions.AsSubQuery{TEntity}"/>.
      /// </summary>
      /// <param name="sqliteOptionsBuilder">SQLite options builder.</param>
      /// <param name="addCustomQueryableMethodTranslatingExpressionVisitorFactory">Indication whether to add a custom factory.</param>
      /// <returns>Provided <paramref name="sqliteOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqliteDbContextOptionsBuilder AddCustomQueryableMethodTranslatingExpressionVisitorFactory([NotNull] this SqliteDbContextOptionsBuilder sqliteOptionsBuilder,
                                                                                                              bool addCustomQueryableMethodTranslatingExpressionVisitorFactory = true)
      {
         return AddOrUpdateExtensions(sqliteOptionsBuilder, extension => extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory = addCustomQueryableMethodTranslatingExpressionVisitorFactory);
      }

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
