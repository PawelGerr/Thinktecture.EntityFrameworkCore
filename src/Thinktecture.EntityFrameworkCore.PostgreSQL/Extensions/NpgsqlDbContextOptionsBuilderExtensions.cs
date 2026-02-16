using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="NpgsqlDbContextOptionsBuilder"/>.
/// </summary>
public static class NpgsqlDbContextOptionsBuilderExtensions
{
   /// <summary>
   /// Adds support for bulk operations and temp tables.
   /// </summary>
   /// <param name="npgsqlOptionsBuilder">Npgsql options builder.</param>
   /// <param name="addBulkOperationSupport">Indication whether to enable or disable the feature.</param>
   /// <param name="configureTempTablesForPrimitiveTypes">Indication whether to configure temp tables for primitive types.</param>
   /// <returns>Provided <paramref name="npgsqlOptionsBuilder"/>.</returns>
   public static NpgsqlDbContextOptionsBuilder AddBulkOperationSupport(
      this NpgsqlDbContextOptionsBuilder npgsqlOptionsBuilder,
      bool addBulkOperationSupport = true,
      bool configureTempTablesForPrimitiveTypes = true)
   {
      return AddOrUpdateExtension(npgsqlOptionsBuilder, extension =>
                                                         {
                                                            extension.AddBulkOperationSupport = addBulkOperationSupport;
                                                            extension.ConfigureTempTablesForPrimitiveTypes = addBulkOperationSupport && configureTempTablesForPrimitiveTypes;
                                                            return extension;
                                                         });
   }

   /// <summary>
   /// Adds support for collection parameters.
   /// </summary>
   /// <param name="npgsqlOptionsBuilder">Npgsql options builder.</param>
   /// <param name="jsonSerializerOptions">JSON serializer options.</param>
   /// <param name="addCollectionParameterSupport">Indication whether to enable or disable the feature.</param>
   /// <param name="configureCollectionParametersForPrimitiveTypes">Indication whether to configure collection parameters for primitive types.</param>
   /// <param name="useDeferredSerialization">Indication whether to use deferred serialization.</param>
   /// <returns>Provided <paramref name="npgsqlOptionsBuilder"/>.</returns>
   public static NpgsqlDbContextOptionsBuilder AddCollectionParameterSupport(
      this NpgsqlDbContextOptionsBuilder npgsqlOptionsBuilder,
      JsonSerializerOptions? jsonSerializerOptions = null,
      bool addCollectionParameterSupport = true,
      bool configureCollectionParametersForPrimitiveTypes = true,
      bool useDeferredSerialization = false)
   {
      return AddOrUpdateExtension(npgsqlOptionsBuilder, extension => extension.AddCollectionParameterSupport(addCollectionParameterSupport, jsonSerializerOptions, configureCollectionParametersForPrimitiveTypes, useDeferredSerialization));
   }

   /// <summary>
   /// Adds custom factory required for translation of custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   /// <param name="builder">Options builder.</param>
   /// <param name="addCustomQueryableMethodTranslatingExpressionVisitorFactory">Indication whether to add a custom factory.</param>
   /// <returns>Provided <paramref name="builder"/>.</returns>
   public static NpgsqlDbContextOptionsBuilder AddCustomQueryableMethodTranslatingExpressionVisitorFactory(
      this NpgsqlDbContextOptionsBuilder builder,
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
   /// Adds support for window functions like "RowNumber".
   /// </summary>
   /// <param name="builder">Options builder.</param>
   /// <param name="addWindowFunctionsSupport">Indication whether to enable or disable the feature.</param>
   /// <returns>Provided <paramref name="builder"/>.</returns>
   public static NpgsqlDbContextOptionsBuilder AddWindowFunctionsSupport(
      this NpgsqlDbContextOptionsBuilder builder,
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
   /// Changes the implementation of <see cref="IMigrationsSqlGenerator"/> to <see cref="ThinktectureNpgsqlMigrationsSqlGenerator"/>.
   /// </summary>
   /// <param name="npgsqlOptionsBuilder">Npgsql options builder.</param>
   /// <param name="useSqlGenerator">Indication whether to enable or disable the feature.</param>
   /// <returns>Provided <paramref name="npgsqlOptionsBuilder"/>.</returns>
   public static NpgsqlDbContextOptionsBuilder UseThinktectureNpgsqlMigrationsSqlGenerator(
      this NpgsqlDbContextOptionsBuilder npgsqlOptionsBuilder,
      bool useSqlGenerator = true)
   {
      return AddOrUpdateExtension(npgsqlOptionsBuilder, extension =>
                                                         {
                                                            extension.UseThinktectureNpgsqlMigrationsSqlGenerator = useSqlGenerator;
                                                            return extension;
                                                         });
   }

   private static NpgsqlDbContextOptionsBuilder AddOrUpdateExtension(this NpgsqlDbContextOptionsBuilder npgsqlOptionsBuilder,
                                                                      Func<NpgsqlDbContextOptionsExtension, NpgsqlDbContextOptionsExtension> callback)
   {
      ArgumentNullException.ThrowIfNull(npgsqlOptionsBuilder);

      var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)npgsqlOptionsBuilder;
      var relationalOptions = infrastructure.OptionsBuilder.TryAddExtension<RelationalDbContextOptionsExtension>();
      infrastructure.OptionsBuilder.AddOrUpdateExtension(callback, () => new NpgsqlDbContextOptionsExtension(relationalOptions));

      return npgsqlOptionsBuilder;
   }
}
