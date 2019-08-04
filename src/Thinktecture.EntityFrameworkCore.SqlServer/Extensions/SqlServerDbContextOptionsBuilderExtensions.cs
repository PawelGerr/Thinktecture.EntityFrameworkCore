using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="SqlServerDbContextOptionsBuilder"/>.
   /// </summary>
   public static class SqlServerDbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Adds support for "RowNumber" and "Descending".
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <param name="addRowNumberSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqlServerDbContextOptionsBuilder AddRowNumberSupport([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                         bool addRowNumberSupport = true)
      {
         ((IRelationalDbContextOptionsBuilderInfrastructure)sqlServerOptionsBuilder).OptionsBuilder.AddDescendingSupport();

         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.AddRowNumberSupport = addRowNumberSupport);
      }

      /// <summary>
      /// Adds support for temp tables.
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <param name="addTempTableSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqlServerDbContextOptionsBuilder AddTempTableSupport([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                         bool addTempTableSupport = true)
      {
         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.AddTempTableSupport = addTempTableSupport);
      }

      /// <summary>
      /// Adds support for bulk operations.
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <param name="addBulkOperationSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqlServerDbContextOptionsBuilder AddBulkOperationSupport([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                             bool addBulkOperationSupport = true)
      {
         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.AddBulkOperationSupport = addBulkOperationSupport);
      }

      /// <summary>
      /// Changes the implementation of <see cref="IMigrationsSqlGenerator"/> to <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <param name="useSqlGenerator">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      [NotNull]
      public static SqlServerDbContextOptionsBuilder UseThinktectureSqlServerMigrationsSqlGenerator([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                                                    bool useSqlGenerator = true)
      {
         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.UseThinktectureSqlServerMigrationsSqlGenerator = useSqlGenerator);
      }

      [NotNull]
      private static SqlServerDbContextOptionsBuilder AddOrUpdateExtensions([NotNull] this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                            [NotNull] Action<SqlServerDbContextOptionsExtension> callback)
      {
         var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)sqlServerOptionsBuilder;
         infrastructure.OptionsBuilder.AddOrUpdateExtension(callback);

         return sqlServerOptionsBuilder;
      }
   }
}
