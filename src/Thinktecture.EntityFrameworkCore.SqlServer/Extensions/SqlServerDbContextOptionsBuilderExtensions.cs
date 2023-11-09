using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="SqlServerDbContextOptionsBuilder"/>.
/// </summary>
public static class SqlServerDbContextOptionsBuilderExtensions
{
   /// <summary>
   /// Adds support for bulk operations and temp tables.
   /// </summary>
   /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
   /// <param name="addBulkOperationSupport">Indication whether to enable or disable the feature.</param>
   /// <param name="configureTempTablesForPrimitiveTypes">Indication whether to configure temp tables for primitive types.</param>
   /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
   public static SqlServerDbContextOptionsBuilder AddBulkOperationSupport(
      this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
      bool addBulkOperationSupport = true,
      bool configureTempTablesForPrimitiveTypes = true)
   {
      return AddOrUpdateExtension(sqlServerOptionsBuilder, extension =>
                                                           {
                                                              extension.AddBulkOperationSupport = addBulkOperationSupport;
                                                              extension.ConfigureTempTablesForPrimitiveTypes = addBulkOperationSupport && configureTempTablesForPrimitiveTypes;
                                                              return extension;
                                                           });
   }

   /// <summary>
   /// Adds support for queryable parameters.
   /// </summary>
   /// <param name="sqlServerOptionsBuilder">SQL Server options builder.</param>
   /// <param name="jsonSerializerOptions">JSON serialization options.</param>
   /// <param name="addCollectionParameterSupport">Indication whether to enable or disable the feature.</param>
   /// <param name="configureCollectionParametersForPrimitiveTypes">Indication whether to configure collection parameters for primitive types.</param>
   /// <param name="useDeferredSerialization">
   /// If <c>true</c> then the provided collection will be serialized when the query is executed.
   /// If <c>false</c> then the collection is going to be serialized when "collection parameter" is created,
   /// i.e. when calling <see cref="BulkOperationsDbContextExtensions.CreateScalarCollectionParameter{T}"/> pr <see cref="BulkOperationsDbContextExtensions.CreateComplexCollectionParameter{T}"/>.
   /// Default is <c>false</c>.
   /// </param>
   /// <returns>Provided <paramref name="sqlServerOptionsBuilder"/>.</returns>
   public static SqlServerDbContextOptionsBuilder AddCollectionParameterSupport(
      this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
      JsonSerializerOptions? jsonSerializerOptions = null,
      bool addCollectionParameterSupport = true,
      bool configureCollectionParametersForPrimitiveTypes = true,
      bool useDeferredSerialization = false)
   {
      return AddOrUpdateExtension(sqlServerOptionsBuilder, extension => extension.AddCollectionParameterSupport(addCollectionParameterSupport, jsonSerializerOptions, configureCollectionParametersForPrimitiveTypes, useDeferredSerialization));
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
      builder.AddOrUpdateExtension(extension =>
                                   {
                                      extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory = addCustomQueryableMethodTranslatingExpressionVisitorFactory;
                                      return extension;
                                   });
      return builder;
   }

   /// <summary>
   /// Adds support for "RowNumber".
   /// </summary>
   /// <param name="builder">Options builder.</param>
   /// <param name="addRowNumberSupport">Indication whether to enable or disable the feature.</param>
   /// <returns>Provided <paramref name="builder"/>.</returns>
   [Obsolete($"Use '{nameof(AddWindowFunctionsSupport)}' instead.")]
   public static SqlServerDbContextOptionsBuilder AddRowNumberSupport(
      this SqlServerDbContextOptionsBuilder builder,
      bool addRowNumberSupport = true)
   {
      builder.AddOrUpdateExtension(extension =>
                                   {
                                      extension.AddWindowFunctionsSupport = addRowNumberSupport;
                                      return extension;
                                   });
      return builder;
   }

   /// <summary>
   /// Adds support for window functions like "RowNumber".
   /// </summary>
   /// <param name="builder">Options builder.</param>
   /// <param name="addWindowFunctionsSupport">Indication whether to enable or disable the feature.</param>
   /// <returns>Provided <paramref name="builder"/>.</returns>
   public static SqlServerDbContextOptionsBuilder AddWindowFunctionsSupport(
      this SqlServerDbContextOptionsBuilder builder,
      bool addWindowFunctionsSupport = true)
   {
      builder.AddOrUpdateExtension(extension =>
                                   {
                                      extension.AddWindowFunctionsSupport = addWindowFunctionsSupport;
                                      return extension;
                                   });
      return builder;
   }

   /// <summary>
   /// Adds support for "Table Hints".
   /// </summary>
   /// <param name="builder">Options builder.</param>
   /// <param name="addTableHintSupport">Indication whether to enable or disable the feature.</param>
   /// <returns>Provided <paramref name="builder"/>.</returns>
   public static SqlServerDbContextOptionsBuilder AddTableHintSupport(
      this SqlServerDbContextOptionsBuilder builder,
      bool addTableHintSupport = true)
   {
      builder.AddOrUpdateExtension(extension =>
                                   {
                                      extension.AddTableHintSupport = addTableHintSupport;
                                      return extension;
                                   });
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
                                      extension.Register(typeof(ITenantDatabaseProviderFactory), typeof(TTenantDatabaseProviderFactory), databaseProviderLifetime);

                                      return extension;
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
      return AddOrUpdateExtension(sqlServerOptionsBuilder, extension =>
                                                           {
                                                              extension.UseThinktectureSqlServerMigrationsSqlGenerator = useSqlGenerator;
                                                              return extension;
                                                           });
   }

   private static SqlServerDbContextOptionsBuilder AddOrUpdateExtension(this SqlServerDbContextOptionsBuilder sqlServerOptionsBuilder,
                                                                        Func<SqlServerDbContextOptionsExtension, SqlServerDbContextOptionsExtension> callback)
   {
      ArgumentNullException.ThrowIfNull(sqlServerOptionsBuilder);

      var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)sqlServerOptionsBuilder;
      var relationalOptions = infrastructure.OptionsBuilder.TryAddExtension<RelationalDbContextOptionsExtension>();
      infrastructure.OptionsBuilder.AddOrUpdateExtension(callback, () => new SqlServerDbContextOptionsExtension(relationalOptions));

      return sqlServerOptionsBuilder;
   }
}
