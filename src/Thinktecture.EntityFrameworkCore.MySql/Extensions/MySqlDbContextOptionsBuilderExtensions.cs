using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Thinktecture extensions for <see cref="MySqlDbContextOptionsBuilder" />.
/// </summary>
public static class MySqlDbContextOptionsBuilderExtensions
{
   /// <summary>
   /// Adds support for window functions.
   /// </summary>
   /// <param name="builder">The MySQL options builder.</param>
   /// <param name="addWindowFunctionsSupport">Indication whether to enable the feature.</param>
   /// <returns>The provided <paramref name="builder" />.</returns>
   public static MySqlDbContextOptionsBuilder AddWindowFunctionsSupport(
      this MySqlDbContextOptionsBuilder builder,
      bool addWindowFunctionsSupport = true)
   {
      return AddOrUpdateExtension(builder, extension =>
                                                    {
                                                       extension.AddWindowFunctionsSupport = addWindowFunctionsSupport;
                                                       return extension;
                                                    });
   }

   /// <summary>
   /// Adds the custom factory required to translate custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   /// <param name="builder">The MySQL options builder.</param>
   /// <param name="addCustomQueryableMethodTranslatingExpressionVisitorFactory">Indication whether to add the custom factory.</param>
   /// <returns>The provided <paramref name="builder" />.</returns>
   public static MySqlDbContextOptionsBuilder AddCustomQueryableMethodTranslatingExpressionVisitorFactory(
      this MySqlDbContextOptionsBuilder builder,
      bool addCustomQueryableMethodTranslatingExpressionVisitorFactory = true)
   {
      return AddOrUpdateExtension(builder, extension =>
                                                    {
                                                       extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory = addCustomQueryableMethodTranslatingExpressionVisitorFactory;
                                                       return extension;
                                                    });
   }

   /// <summary>
   /// Adds support for MySQL bulk inserts.
   /// </summary>
   /// <param name="builder">The MySQL options builder.</param>
   /// <param name="addBulkOperationSupport">Indication whether to enable the feature.</param>
   /// <param name="configureTempTablesForPrimitiveTypes">Indication whether to configure temp table entities for primitive CLR types.</param>
   /// <returns>The provided <paramref name="builder" />.</returns>
   public static MySqlDbContextOptionsBuilder AddBulkOperationSupport(
      this MySqlDbContextOptionsBuilder builder,
      bool addBulkOperationSupport = true,
      bool configureTempTablesForPrimitiveTypes = true)
   {
      return AddOrUpdateExtension(builder, extension =>
                                                    {
                                                       extension.AddBulkOperationSupport = addBulkOperationSupport;
                                                       extension.ConfigureTempTablesForPrimitiveTypes = addBulkOperationSupport && configureTempTablesForPrimitiveTypes;
                                                       return extension;
                                                    });
   }

   /// <summary>
   /// Adds support for collection parameters using MySQL JSON_TABLE.
   /// </summary>
   /// <param name="builder">The MySQL options builder.</param>
   /// <param name="jsonSerializerOptions">JSON serialization options.</param>
   /// <param name="addCollectionParameterSupport">Indication whether to enable the feature.</param>
   /// <param name="configureCollectionParametersForPrimitiveTypes">Indication whether to configure primitive CLR collection types.</param>
   /// <returns>The provided <paramref name="builder" />.</returns>
   public static MySqlDbContextOptionsBuilder AddCollectionParameterSupport(
      this MySqlDbContextOptionsBuilder builder,
      JsonSerializerOptions? jsonSerializerOptions = null,
      bool addCollectionParameterSupport = true,
      bool configureCollectionParametersForPrimitiveTypes = true)
   {
      return AddOrUpdateExtension(builder, extension => extension.AddCollectionParameterSupport(
                                                               addCollectionParameterSupport,
                                                               jsonSerializerOptions,
                                                               configureCollectionParametersForPrimitiveTypes));
   }

   private static MySqlDbContextOptionsBuilder AddOrUpdateExtension(
      MySqlDbContextOptionsBuilder builder,
      Func<MySqlDbContextOptionsExtension, MySqlDbContextOptionsExtension> callback)
   {
      ArgumentNullException.ThrowIfNull(builder);

      var infrastructure = (IRelationalDbContextOptionsBuilderInfrastructure)builder;
      var relationalOptions = infrastructure.OptionsBuilder.TryAddExtension<RelationalDbContextOptionsExtension>();
      infrastructure.OptionsBuilder.AddOrUpdateExtension(callback, () => new MySqlDbContextOptionsExtension(relationalOptions));

      return builder;
   }
}