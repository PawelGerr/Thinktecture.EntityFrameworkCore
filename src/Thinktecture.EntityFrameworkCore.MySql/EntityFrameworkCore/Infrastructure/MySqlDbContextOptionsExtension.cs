using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Query.Internal;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;
using Microting.EntityFrameworkCore.MySql.Query.ExpressionVisitors.Internal;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Extensions for MySQL DbContext options.
/// </summary>
public sealed class MySqlDbContextOptionsExtension : DbContextOptionsExtensionBase, IDbContextOptionsExtension
{
   private readonly RelationalDbContextOptionsExtension _relationalOptions;
   private MySqlDbContextOptionsExtensionInfo? _info;
   private JsonSerializerOptions? _collectionParameterJsonSerializerOptions;
   private bool _addCollectionParameterSupport;

   /// <inheritdoc />
   public DbContextOptionsExtensionInfo Info => _info ??= new MySqlDbContextOptionsExtensionInfo(this);

   /// <summary>
   /// Enables support for window functions.
   /// </summary>
   public bool AddWindowFunctionsSupport
   {
      get => _relationalOptions.AddWindowFunctionsSupport;
      set => _relationalOptions.AddWindowFunctionsSupport = value;
   }

   /// <summary>
   /// Enables support for MySQL bulk operations.
   /// </summary>
   public bool AddBulkOperationSupport { get; set; }

   private bool _addCustomQueryableMethodTranslatingExpressionVisitorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required to translate custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   public bool AddCustomQueryableMethodTranslatingExpressionVisitorFactory
   {
      get => _addCustomQueryableMethodTranslatingExpressionVisitorFactory || AddBulkOperationSupport || AddWindowFunctionsSupport || _addCollectionParameterSupport;
      set => _addCustomQueryableMethodTranslatingExpressionVisitorFactory = value;
   }

   private bool _addCustomQuerySqlGeneratorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required for window functions and bulk operations.
   /// </summary>
   public bool AddCustomQuerySqlGeneratorFactory
   {
      get => _addCustomQuerySqlGeneratorFactory || AddBulkOperationSupport || AddWindowFunctionsSupport;
      set => _addCustomQuerySqlGeneratorFactory = value;
   }

   private bool _addCustomRelationalParameterBasedSqlProcessorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required for window functions and bulk operations.
   /// </summary>
   public bool AddCustomRelationalParameterBasedSqlProcessorFactory
   {
      get => _addCustomRelationalParameterBasedSqlProcessorFactory || AddBulkOperationSupport || AddWindowFunctionsSupport;
      set => _addCustomRelationalParameterBasedSqlProcessorFactory = value;
   }

   /// <summary>
   /// Enables temp table conventions for primitive types.
   /// </summary>
   public bool ConfigureTempTablesForPrimitiveTypes { get; set; }

   /// <summary>
   /// Enables temp table conventions for primitive collection parameter types.
   /// </summary>
   internal bool ConfigureCollectionParametersForPrimitiveTypes { get; private set; }

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlDbContextOptionsExtension" />.
   /// </summary>
   /// <param name="relationalOptions">The relational options extension.</param>
   public MySqlDbContextOptionsExtension(RelationalDbContextOptionsExtension relationalOptions)
   {
      _relationalOptions = relationalOptions ?? throw new ArgumentNullException(nameof(relationalOptions));
   }

   /// <inheritdoc />
   [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
   public void ApplyServices(IServiceCollection services)
   {
      services.TryAddSingleton<MySqlDbContextOptionsExtensionOptions>();
      services.AddSingleton<ISingletonOptions>(provider => provider.GetRequiredService<MySqlDbContextOptionsExtensionOptions>());

      services.Add<IMethodCallTranslatorPlugin, MySqlMethodCallTranslatorPlugin>(GetLifetime<IMethodCallTranslatorPlugin>());
      services.Add<IAggregateMethodCallTranslatorPlugin, MySqlAggregateMethodCallTranslatorPlugin>(GetLifetime<IAggregateMethodCallTranslatorPlugin>());

      if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
         AddWithCheck<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureMySqlQueryableMethodTranslatingExpressionVisitorFactory, MySqlQueryableMethodTranslatingExpressionVisitorFactory>(services);

      if (AddCustomQuerySqlGeneratorFactory)
         AddWithCheck<IQuerySqlGeneratorFactory, ThinktectureMySqlQuerySqlGeneratorFactory, MySqlQuerySqlGeneratorFactory>(services);

      if (AddCustomRelationalParameterBasedSqlProcessorFactory)
         AddWithCheck<IRelationalParameterBasedSqlProcessorFactory, ThinktectureMySqlParameterBasedSqlProcessorFactory, MySqlParameterBasedSqlProcessorFactory>(services);

      if (AddWindowFunctionsSupport)
      {
         var lifetime = GetLifetime<IEvaluatableExpressionFilterPlugin>();
         services.Add(ServiceDescriptor.Describe(typeof(IEvaluatableExpressionFilterPlugin), typeof(MySqlWindowFunctionEvaluatableExpressionFilterPlugin), lifetime));
      }

      if (AddBulkOperationSupport)
      {
         services.AddSingleton<IBulkOperationsDbContextOptionsExtensionOptions>(provider => provider.GetRequiredService<MySqlDbContextOptionsExtensionOptions>());
         services.Add<IConventionSetPlugin, BulkOperationConventionSetPlugin>(GetLifetime<IConventionSetPlugin>());

         AddEntityDataReader(services);
         services.TryAddScoped<MySqlBulkOperationExecutor>();
         services.TryAddScoped<IBulkInsertExecutor>(provider => provider.GetRequiredService<MySqlBulkOperationExecutor>());
         services.TryAddScoped<ITempTableBulkInsertExecutor>(provider => provider.GetRequiredService<MySqlBulkOperationExecutor>());
         services.TryAddScoped<IBulkUpdateExecutor>(provider => provider.GetRequiredService<MySqlBulkOperationExecutor>());
         services.TryAddScoped<IBulkInsertOrUpdateExecutor>(provider => provider.GetRequiredService<MySqlBulkOperationExecutor>());
         services.TryAddScoped<ITruncateTableExecutor>(provider => provider.GetRequiredService<MySqlBulkOperationExecutor>());
         services.TryAddScoped<ITempTableCreator, MySqlTempTableCreator>();
         services.AddTempTableSuffixComponents();
      }

      if (_addCollectionParameterSupport)
      {
         var jsonSerializerOptions = _collectionParameterJsonSerializerOptions ?? new JsonSerializerOptions();
         services.AddSingleton<ICollectionParameterFactory>(provider => ActivatorUtilities.CreateInstance<MySqlCollectionParameterFactory>(provider, jsonSerializerOptions));
         services.Add<IConventionSetPlugin, MySqlCollectionParameterConventionSetPlugin>(GetLifetime<IConventionSetPlugin>());
      }
   }

   /// <inheritdoc />
   [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
   public void Validate(IDbContextOptions options)
   {
      ArgumentNullException.ThrowIfNull(options);

      var serverVersion = options.FindExtension<MySqlOptionsExtension>()?.ServerVersion;

      if (serverVersion is null)
         return;

      if (AddWindowFunctionsSupport && !serverVersion.Supports.WindowFunctions)
         throw new InvalidOperationException($"Window functions require MySQL 8.0 or MariaDB 10.2, but the configured server version is {serverVersion}.");

      if (_addCollectionParameterSupport && !serverVersion.Supports.JsonTable)
         throw new InvalidOperationException($"Collection parameters use JSON_TABLE, which requires MySQL 8.0.4 or MariaDB 10.6, but the configured server version is {serverVersion}.");
   }

   /// <summary>
   /// Configures MySQL collection parameter support.
   /// </summary>
   /// <param name="addCollectionParameterSupport">Indication whether to enable the feature.</param>
   /// <param name="jsonSerializerOptions">JSON serialization options.</param>
   /// <param name="configureCollectionParametersForPrimitiveTypes">Indication whether to configure primitive CLR collection types.</param>
   /// <returns>This extension.</returns>
   public MySqlDbContextOptionsExtension AddCollectionParameterSupport(
      bool addCollectionParameterSupport,
      JsonSerializerOptions? jsonSerializerOptions,
      bool configureCollectionParametersForPrimitiveTypes)
   {
      _addCollectionParameterSupport = addCollectionParameterSupport;
      _collectionParameterJsonSerializerOptions = jsonSerializerOptions;
      ConfigureCollectionParametersForPrimitiveTypes = addCollectionParameterSupport && configureCollectionParametersForPrimitiveTypes;
      return this;
   }

   private sealed class MySqlDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
   {
      private readonly MySqlDbContextOptionsExtension _extension;
      public override bool IsDatabaseProvider => false;

      /// <inheritdoc />
      public override string LogFragment => (_extension.AddBulkOperationSupport ? "BulkOperationSupport " : String.Empty)
                          + (_extension._addCollectionParameterSupport ? "CollectionParameterSupport " : String.Empty)
                          + (_extension.AddWindowFunctionsSupport ? "WindowFunctionsSupport " : String.Empty);

      public MySqlDbContextOptionsExtensionInfo(MySqlDbContextOptionsExtension extension)
         : base(extension)
      {
         _extension = extension ?? throw new ArgumentNullException(nameof(extension));
      }

      /// <inheritdoc />
      public override int GetServiceProviderHashCode()
      {
         var hashCode = new HashCode();
         hashCode.Add(_extension.AddBulkOperationSupport);
         hashCode.Add(_extension.ConfigureTempTablesForPrimitiveTypes);
         hashCode.Add(_extension.AddWindowFunctionsSupport);
         hashCode.Add(_extension._addCollectionParameterSupport);
         hashCode.Add(_extension.ConfigureCollectionParametersForPrimitiveTypes);
         hashCode.Add(_extension._collectionParameterJsonSerializerOptions);
         hashCode.Add(_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory);
         hashCode.Add(_extension.AddCustomQuerySqlGeneratorFactory);
         hashCode.Add(_extension.AddCustomRelationalParameterBasedSqlProcessorFactory);
         return hashCode.ToHashCode();
      }

      /// <inheritdoc />
      public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
      {
         return other is MySqlDbContextOptionsExtensionInfo otherMySqlInfo
              && _extension.AddBulkOperationSupport == otherMySqlInfo._extension.AddBulkOperationSupport
                && _extension.ConfigureTempTablesForPrimitiveTypes == otherMySqlInfo._extension.ConfigureTempTablesForPrimitiveTypes
                        && _extension._addCollectionParameterSupport == otherMySqlInfo._extension._addCollectionParameterSupport
                        && _extension.ConfigureCollectionParametersForPrimitiveTypes == otherMySqlInfo._extension.ConfigureCollectionParametersForPrimitiveTypes
                        && _extension._collectionParameterJsonSerializerOptions == otherMySqlInfo._extension._collectionParameterJsonSerializerOptions
                && _extension.AddWindowFunctionsSupport == otherMySqlInfo._extension.AddWindowFunctionsSupport
                && _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory == otherMySqlInfo._extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory
                && _extension.AddCustomQuerySqlGeneratorFactory == otherMySqlInfo._extension.AddCustomQuerySqlGeneratorFactory
                && _extension.AddCustomRelationalParameterBasedSqlProcessorFactory == otherMySqlInfo._extension.AddCustomRelationalParameterBasedSqlProcessorFactory;
      }

      /// <inheritdoc />
      public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
      {
         debugInfo["Thinktecture:MySqlWindowFunctionsSupport"] = _extension.AddWindowFunctionsSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlBulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlConfigureTempTablesForPrimitiveTypes"] = _extension.ConfigureTempTablesForPrimitiveTypes.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlCollectionParameterSupport"] = _extension._addCollectionParameterSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlConfigureCollectionParametersForPrimitiveTypes"] = _extension.ConfigureCollectionParametersForPrimitiveTypes.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlCollectionParameterJsonSerializerOptions"] = (_extension._collectionParameterJsonSerializerOptions?.GetHashCode() ?? 0).ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlCustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlCustomQuerySqlGeneratorFactory"] = _extension.AddCustomQuerySqlGeneratorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:MySqlCustomRelationalParameterBasedSqlProcessorFactory"] = _extension.AddCustomRelationalParameterBasedSqlProcessorFactory.ToString(CultureInfo.InvariantCulture);
      }
   }
}