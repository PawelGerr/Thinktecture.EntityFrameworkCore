using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query;

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
         return AddOrUpdateExtension(sqlServerOptionsBuilder, extension => extension.AddTempTableSupport = addTempTableSupport);
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
         return AddOrUpdateExtension(sqlServerOptionsBuilder, extension => extension.AddBulkOperationSupport = addBulkOperationSupport);
      }

      /// <summary>
      /// Adds custom factory required for translation of custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addCustomQueryableMethodTranslatingExpressionVisitorFactory">Indication whether to add a custom factory.</param>
      /// <returns>Provided <paramref name="builder"/>.</returns>
      public static SqlServerDbContextOptionsBuilder AddCustomQueryableMethodTranslatingExpressionVisitorFactory(
         this SqlServerDbContextOptionsBuilder builder,
         bool addCustomQueryableMethodTranslatingExpressionVisitorFactory = true)
      {
         builder.AddOrUpdateExtension(extension => extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory = addCustomQueryableMethodTranslatingExpressionVisitorFactory);
         return builder;
      }

      /// <summary>
      /// Adds support for "RowNumber".
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addRowNumberSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="builder"/>.</returns>
      public static SqlServerDbContextOptionsBuilder AddRowNumberSupport(
         this SqlServerDbContextOptionsBuilder builder,
         bool addRowNumberSupport = true)
      {
         builder.AddOrUpdateExtension(extension => extension.AddRowNumberSupport = addRowNumberSupport);
         return builder;
      }

      /// <summary>
      /// Adds 'tenant database support'.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addTenantSupport">Indication whether to enable or disable the feature.</param>
      /// <param name="databaseProviderLifetime">The lifetime of the provided <typeparamref name="TTenantDatabaseProviderFactory"/>.</param>
      /// <returns>Provided <paramref name="builder"/>.</returns>
      public static SqlServerDbContextOptionsBuilder AddTenantDatabaseSupport<TTenantDatabaseProviderFactory>(
         this SqlServerDbContextOptionsBuilder builder,
         bool addTenantSupport = true,
         ServiceLifetime databaseProviderLifetime = ServiceLifetime.Singleton)
         where TTenantDatabaseProviderFactory : ITenantDatabaseProviderFactory
      {
         builder.AddOrUpdateExtension(extension =>
                                      {
                                         extension.AddTenantDatabaseSupport = addTenantSupport;
                                         extension.Add(ServiceDescriptor.Describe(typeof(ITenantDatabaseProviderFactory), typeof(TTenantDatabaseProviderFactory), databaseProviderLifetime));
                                      });
         return builder;
      }

      /// <summary>
      /// Changes the implementation of <see cref="IMigrationsSqlGenerator"/> to <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
      /// </summary>
      /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
      /// <param name="useSqlGenerator">Indication whether to enable or disable the feature.</param>
      /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
      public static SqlServerDbContextOptionsBuilder UseThinktectureSqlServerMigrationsSqlGenerator(
         this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
         bool useSqlGenerator = true)
      {
         return AddOrUpdateExtension(sqlServerOptionsBuilder, extension => extension.UseThinktectureSqlServerMigrationsSqlGenerator = useSqlGenerator);
      }

      private static SqlServerDbContextOptionsBuilder AddOrUpdateExtension(this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                           Action<SqlServerDbContextOptionsExtension> callback)
      {
         if (sqlServerOptionsBuilder == null)
            throw new ArgumentNullException(nameof(sqlServerOptionsBuilder));

         var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)sqlServerOptionsBuilder;
         var relationalOptions = infrastructure.OptionsBuilder.TryAddExtension<RelationalDbContextOptionsExtension>();
         infrastructure.OptionsBuilder.AddOrUpdateExtension(callback, () => new SqlServerDbContextOptionsExtension(relationalOptions));

         return sqlServerOptionsBuilder;
      }
   }
}
