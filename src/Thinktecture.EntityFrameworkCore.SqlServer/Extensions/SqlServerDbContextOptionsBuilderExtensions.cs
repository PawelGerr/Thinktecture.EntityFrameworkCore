using System;
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
      /// Adds support for temp tables.
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <param name="addTempTableSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      public static SqlServerDbContextOptionsBuilder AddTempTableSupport(this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
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
      public static SqlServerDbContextOptionsBuilder AddBulkOperationSupport(this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
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
      public static SqlServerDbContextOptionsBuilder UseThinktectureSqlServerMigrationsSqlGenerator(this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                                                    bool useSqlGenerator = true)
      {
         return AddOrUpdateExtensions(sqlServerOptionsBuilder, extension => extension.UseThinktectureSqlServerMigrationsSqlGenerator = useSqlGenerator);
      }

      private static SqlServerDbContextOptionsBuilder AddOrUpdateExtensions(this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                            Action<SqlServerDbContextOptionsExtension> callback)
      {
         if (sqlServerOptionsBuilder == null)
            throw new ArgumentNullException(nameof(sqlServerOptionsBuilder));

         var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)sqlServerOptionsBuilder;
         infrastructure.OptionsBuilder.AddOrUpdateExtension(callback);

         return sqlServerOptionsBuilder;
      }
   }
}
