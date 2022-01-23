using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Extensions for DbContextOptions.
/// </summary>
public sealed class SqlServerDbContextOptionsExtension : DbContextOptionsExtensionBase, IDbContextOptionsExtension
{
   private readonly RelationalDbContextOptionsExtension _relationalOptions;

   private SqlServerDbContextOptionsExtensionInfo? _info;

   /// <inheritdoc />
   public DbContextOptionsExtensionInfo Info => _info ??= new SqlServerDbContextOptionsExtensionInfo(this);

   /// <summary>
   /// Enables and disables support for "RowNumber".
   /// </summary>
   public bool AddRowNumberSupport
   {
      get => _relationalOptions.AddRowNumberSupport;
      set => _relationalOptions.AddRowNumberSupport = value;
   }

   /// <summary>
   /// Enables and disables tenants support.
   /// </summary>
   public bool AddTenantDatabaseSupport
   {
      get => _relationalOptions.AddTenantDatabaseSupport;
      set => _relationalOptions.AddTenantDatabaseSupport = value;
   }

   private bool _addCustomQueryableMethodTranslatingExpressionVisitorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required to be able to translate custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
   /// </summary>
   public bool AddCustomQueryableMethodTranslatingExpressionVisitorFactory
   {
      get => _addCustomQueryableMethodTranslatingExpressionVisitorFactory || AddBulkOperationSupport || AddRowNumberSupport || AddTableHintSupport;
      set => _addCustomQueryableMethodTranslatingExpressionVisitorFactory = value;
   }

   private bool _addCustomRelationalParameterBasedSqlProcessorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required for some features.
   /// </summary>
   public bool AddCustomRelationalParameterBasedSqlProcessorFactory
   {
      get => _addCustomRelationalParameterBasedSqlProcessorFactory || AddBulkOperationSupport || AddRowNumberSupport || AddTableHintSupport;
      set => _addCustomRelationalParameterBasedSqlProcessorFactory = value;
   }

   private bool _addCustomQuerySqlGeneratorFactory;

   /// <summary>
   /// A custom factory is registered if <c>true</c>.
   /// The factory is required for some features like 'tenant database support' or for generation of 'DELETE' statements.
   /// </summary>
   public bool AddCustomQuerySqlGeneratorFactory
   {
      get => _addCustomQuerySqlGeneratorFactory || AddBulkOperationSupport || AddTenantDatabaseSupport || AddTableHintSupport;
      set => _addCustomQuerySqlGeneratorFactory = value;
   }

   /// <summary>
   /// Enables and disables support for table hints.
   /// </summary>
   public bool AddTableHintSupport { get; set; }

   /// <summary>
   /// Enables and disables support for bulk operations and temp tables.
   /// </summary>
   public bool AddBulkOperationSupport { get; set; }

   private JsonSerializerOptions? _collectionParameterJsonSerializerOptions;
   private bool _addCollectionParameterSupport;

   /// <summary>
   /// Changes the implementation of <see cref="IMigrationsSqlGenerator"/> to <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
   /// </summary>
   public bool UseThinktectureSqlServerMigrationsSqlGenerator { get; set; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerDbContextOptionsExtension"/>.
   /// </summary>
   /// <param name="relationalOptions">An instance of <see cref="RelationalDbContextOptionsExtension"/>.</param>
   public SqlServerDbContextOptionsExtension(RelationalDbContextOptionsExtension relationalOptions)
   {
      _relationalOptions = relationalOptions ?? throw new ArgumentNullException(nameof(relationalOptions));
   }

   /// <inheritdoc />
   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   public void ApplyServices(IServiceCollection services)
   {
      if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
         AddWithCheck<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory, SqlServerQueryableMethodTranslatingExpressionVisitorFactory>(services);

      if (AddCustomQuerySqlGeneratorFactory)
         AddWithCheck<IQuerySqlGeneratorFactory, ThinktectureSqlServerQuerySqlGeneratorFactory, SqlServerQuerySqlGeneratorFactory>(services);

      if (AddCustomRelationalParameterBasedSqlProcessorFactory)
         AddWithCheck<IRelationalParameterBasedSqlProcessorFactory, ThinktectureSqlServerParameterBasedSqlProcessorFactory, SqlServerParameterBasedSqlProcessorFactory>(services);

      if (AddBulkOperationSupport)
      {
         services.Add<IConventionSetPlugin, BulkOperationConventionSetPlugin>(GetLifetime<IConventionSetPlugin>());

         services.AddSingleton<TempTableStatementCache<SqlServerTempTableCreatorCacheKey>>();
         services.AddSingleton<TempTableStatementCache<SqlServerTempTablePrimaryKeyCacheKey>>();
         services.TryAddScoped<ISqlServerTempTableCreator, SqlServerTempTableCreator>();
         services.TryAddScoped<ITempTableCreator>(provider => provider.GetRequiredService<ISqlServerTempTableCreator>());
         services.AddTempTableSuffixComponents();

         AddEntityDataReader(services);
         services.TryAddScoped<SqlServerBulkOperationExecutor>();
         services.TryAddScoped<IBulkInsertExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
         services.TryAddScoped<ITempTableBulkInsertExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
         services.TryAddScoped<IBulkUpdateExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
         services.TryAddScoped<IBulkInsertOrUpdateExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
         services.TryAddScoped<ITruncateTableExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
      }

      if (_addCollectionParameterSupport)
      {
         var jsonSerializerOptions = _collectionParameterJsonSerializerOptions ?? new JsonSerializerOptions();

         services.AddSingleton<ICollectionParameterFactory>(serviceProvider => new SqlServerCollectionParameterFactory(jsonSerializerOptions,
                                                                                                                       serviceProvider.GetRequiredService<ObjectPool<StringBuilder>>(),
                                                                                                                       serviceProvider.GetRequiredService<ISqlGenerationHelper>()));
         services.Add<IConventionSetPlugin, SqlServerCollectionParameterConventionSetPlugin>(GetLifetime<IConventionSetPlugin>());
      }

      if (UseThinktectureSqlServerMigrationsSqlGenerator)
         AddWithCheck<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator, SqlServerMigrationsSqlGenerator>(services);

      if (_relationalOptions.AddSchemaRespectingComponents)
         services.AddSingleton<IMigrationOperationSchemaSetter, SqlServerMigrationOperationSchemaSetter>();
   }

   /// <summary>
   /// Registers a custom service with internal dependency injection container of Entity Framework Core.
   /// </summary>
   /// <param name="serviceType">Service type.</param>
   /// <param name="implementationType">Implementation type.</param>
   /// <param name="lifetime">Service lifetime.</param>
   /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> or <paramref name="implementationType"/> is <c>null</c>.</exception>
   public void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime)
   {
      _relationalOptions.Register(serviceType, implementationType, lifetime);
   }

   /// <summary>
   /// Registers a custom service instance with internal dependency injection container of Entity Framework Core.
   /// </summary>
   /// <param name="serviceType">Service type.</param>
   /// <param name="implementationInstance">Implementation instance.</param>
   /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> or <paramref name="implementationInstance"/> is <c>null</c>.</exception>
   public void Register(Type serviceType, object implementationInstance)
   {
      _relationalOptions.Register(serviceType, implementationInstance);
   }

   /// <summary>
   /// Enables and disables support for queryable parameters.
   /// </summary>
   public SqlServerDbContextOptionsExtension AddCollectionParameterSupport(bool addCollectionParameterSupport, JsonSerializerOptions? jsonSerializerOptions)
   {
      _addCollectionParameterSupport = addCollectionParameterSupport;
      _collectionParameterJsonSerializerOptions = jsonSerializerOptions;

      return this;
   }

   /// <inheritdoc />
   public void Validate(IDbContextOptions options)
   {
   }

   private class SqlServerDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
   {
      private readonly SqlServerDbContextOptionsExtension _extension;
      public override bool IsDatabaseProvider => false;

      private string? _logFragment;

      public override string LogFragment => _logFragment ??= CreateLogFragment();

      private string CreateLogFragment()
      {
         var sb = new StringBuilder();

         if (_extension.AddBulkOperationSupport)
            sb.Append("BulkOperationSupport ");

         if (_extension._addCollectionParameterSupport)
            sb.Append("CollectionParameterSupport ");

         if (_extension.AddTableHintSupport)
            sb.Append("TableHintSupport ");

         if (_extension.UseThinktectureSqlServerMigrationsSqlGenerator)
            sb.Append("ThinktectureSqlServerMigrationsSqlGenerator ");

         return sb.ToString();
      }

      /// <inheritdoc />
      public SqlServerDbContextOptionsExtensionInfo(SqlServerDbContextOptionsExtension extension)
         : base(extension)
      {
         _extension = extension ?? throw new ArgumentNullException(nameof(extension));
      }

      /// <inheritdoc />
      public override int GetServiceProviderHashCode()
      {
         var hashCode = new HashCode();

         hashCode.Add(_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory);
         hashCode.Add(_extension.AddCustomQuerySqlGeneratorFactory);
         hashCode.Add(_extension.AddCustomRelationalParameterBasedSqlProcessorFactory);
         hashCode.Add(_extension.AddBulkOperationSupport);
         hashCode.Add(_extension._addCollectionParameterSupport);
         hashCode.Add(_extension._collectionParameterJsonSerializerOptions);
         hashCode.Add(_extension.AddTenantDatabaseSupport);
         hashCode.Add(_extension.AddTableHintSupport);
         hashCode.Add(_extension.UseThinktectureSqlServerMigrationsSqlGenerator);

         return hashCode.ToHashCode();
      }

      /// <inheritdoc />
      public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
      {
         return other is SqlServerDbContextOptionsExtensionInfo otherSqlServerInfo
                && _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory == otherSqlServerInfo._extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory
                && _extension.AddCustomQuerySqlGeneratorFactory == otherSqlServerInfo._extension.AddCustomQuerySqlGeneratorFactory
                && _extension.AddCustomRelationalParameterBasedSqlProcessorFactory == otherSqlServerInfo._extension.AddCustomRelationalParameterBasedSqlProcessorFactory
                && _extension.AddBulkOperationSupport == otherSqlServerInfo._extension.AddBulkOperationSupport
                && _extension._addCollectionParameterSupport == otherSqlServerInfo._extension._addCollectionParameterSupport
                && _extension._collectionParameterJsonSerializerOptions == otherSqlServerInfo._extension._collectionParameterJsonSerializerOptions
                && _extension.AddTenantDatabaseSupport == otherSqlServerInfo._extension.AddTenantDatabaseSupport
                && _extension.AddTableHintSupport == otherSqlServerInfo._extension.AddTableHintSupport
                && _extension.UseThinktectureSqlServerMigrationsSqlGenerator == otherSqlServerInfo._extension.UseThinktectureSqlServerMigrationsSqlGenerator;
      }

      /// <inheritdoc />
      public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
      {
         debugInfo["Thinktecture:CustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:CustomQuerySqlGeneratorFactory"] = _extension.AddCustomQuerySqlGeneratorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:CustomRelationalParameterBasedSqlProcessorFactory"] = _extension.AddCustomRelationalParameterBasedSqlProcessorFactory.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:CollectionParameterSupport"] = _extension._addCollectionParameterSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:TenantDatabaseSupport"] = _extension.AddTenantDatabaseSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:TableHintSupport"] = _extension.AddTableHintSupport.ToString(CultureInfo.InvariantCulture);
         debugInfo["Thinktecture:UseThinktectureSqlServerMigrationsSqlGenerator"] = _extension.UseThinktectureSqlServerMigrationsSqlGenerator.ToString(CultureInfo.InvariantCulture);
      }
   }
}
