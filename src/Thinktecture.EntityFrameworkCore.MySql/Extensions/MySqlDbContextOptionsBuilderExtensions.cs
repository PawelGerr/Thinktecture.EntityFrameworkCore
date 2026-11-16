using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.BulkOperations;
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
   /// Adds support for MySQL bulk operations (bulk insert, bulk update, upsert, temp tables and truncate).
   /// </summary>
   /// <param name="builder">The MySQL options builder.</param>
   /// <param name="addBulkOperationSupport">Indication whether to enable the feature.</param>
   /// <param name="configureTempTablesForPrimitiveTypes">Indication whether to configure temp table entities for primitive CLR types.</param>
   /// <param name="useBulkCopy">
   /// Indication whether every insert of the bulk operations uses <see cref="MySqlConnector.MySqlBulkCopy" /> (<c>LOAD DATA LOCAL INFILE</c>) instead of multi-row INSERT statements.
   /// This also applies to temp tables and to the staging tables of bulk update and upsert.
   /// The connection must enable <c>AllowLoadLocalInfile=true</c>, and the server must enable <c>local_infile</c>.
   /// The rows are streamed to the server. Because <c>LOAD DATA LOCAL</c> turns errors into warnings, a warning of the level <c>Warning</c> or <c>Error</c> and any difference between the read and the inserted rows throw an exception. A warning of the level <c>Note</c>, for example the rounding of a decimal to the scale of the column, is ignored, as on the INSERT path.
   /// </param>
   /// <param name="maxParametersPerStatement">
   /// Maximum number of parameters of one multi-row INSERT statement; the number of rows per statement is this value divided by the number of columns, at least 1.
   /// The value is between 1 and 65,535 (the limit of a prepared statement). The default is 5,000. It can be overridden per operation and is ignored if <paramref name="useBulkCopy" /> is <c>true</c>.
   /// </param>
   /// <returns>The provided <paramref name="builder" />.</returns>
   /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxParametersPerStatement" /> is less than 1 or greater than 65,535.</exception>
   public static MySqlDbContextOptionsBuilder AddBulkOperationSupport(
      this MySqlDbContextOptionsBuilder builder,
      bool addBulkOperationSupport = true,
      bool configureTempTablesForPrimitiveTypes = true,
      bool useBulkCopy = false,
      int maxParametersPerStatement = 5_000)
   {
      MySqlParameterBudget.Validate(maxParametersPerStatement);

      return AddOrUpdateExtension(builder, extension =>
                                                    {
                                                       extension.AddBulkOperationSupport = addBulkOperationSupport;
                                                       extension.ConfigureTempTablesForPrimitiveTypes = addBulkOperationSupport && configureTempTablesForPrimitiveTypes;
                                                       extension.UseBulkCopy = useBulkCopy;
                                                       extension.MaxParametersPerStatement = maxParametersPerStatement;
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