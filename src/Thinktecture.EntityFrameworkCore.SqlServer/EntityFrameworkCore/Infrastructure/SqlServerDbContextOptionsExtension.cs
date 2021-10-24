using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
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
      public void ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);

         if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
            AddWithCheck<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory, SqlServerQueryableMethodTranslatingExpressionVisitorFactory>(services);

         if (AddCustomQuerySqlGeneratorFactory)
            AddWithCheck<IQuerySqlGeneratorFactory, ThinktectureSqlServerQuerySqlGeneratorFactory, SqlServerQuerySqlGeneratorFactory>(services);

         if (AddCustomRelationalParameterBasedSqlProcessorFactory)
            AddWithCheck<IRelationalParameterBasedSqlProcessorFactory, ThinktectureSqlServerParameterBasedSqlProcessorFactory, SqlServerParameterBasedSqlProcessorFactory>(services);

         if (AddBulkOperationSupport)
         {
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

         if (UseThinktectureSqlServerMigrationsSqlGenerator)
            AddWithCheck<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator, SqlServerMigrationsSqlGenerator>(services);
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

      /// <inheritdoc />
      public void Validate(IDbContextOptions options)
      {
      }

      private class SqlServerDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
      {
         private readonly SqlServerDbContextOptionsExtension _extension;
         public override bool IsDatabaseProvider => false;

         private string? _logFragment;

         public override string LogFragment => _logFragment ??= $@"
{{
   'Custom QueryableMethodTranslatingExpressionVisitorFactory'={_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory},
   'Custom QuerySqlGeneratorFactory'={_extension.AddCustomQuerySqlGeneratorFactory},
   'Custom RelationalParameterBasedSqlProcessorFactory'={_extension.AddCustomRelationalParameterBasedSqlProcessorFactory},
   'BulkOperationSupport'={_extension.AddBulkOperationSupport},
   'TenantDatabaseSupport'={_extension.AddTenantDatabaseSupport},
   'TableHintSupport'={_extension.AddTableHintSupport},
   'UseThinktectureSqlServerMigrationsSqlGenerator'={_extension.UseThinktectureSqlServerMigrationsSqlGenerator}
}}";

         /// <inheritdoc />
         public SqlServerDbContextOptionsExtensionInfo(SqlServerDbContextOptionsExtension extension)
            : base(extension)
         {
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
         }

         /// <inheritdoc />
         public override int GetServiceProviderHashCode()
         {
            return HashCode.Combine(_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory,
                                    _extension.AddCustomQuerySqlGeneratorFactory,
                                    _extension.AddCustomRelationalParameterBasedSqlProcessorFactory,
                                    _extension.AddBulkOperationSupport,
                                    _extension.AddTenantDatabaseSupport,
                                    _extension.AddTableHintSupport,
                                    _extension.UseThinktectureSqlServerMigrationsSqlGenerator);
         }

         /// <inheritdoc />
         public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
         {
            return other is SqlServerDbContextOptionsExtensionInfo otherSqlServerInfo
                   && _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory == otherSqlServerInfo._extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory
                   && _extension.AddCustomQuerySqlGeneratorFactory == otherSqlServerInfo._extension.AddCustomQuerySqlGeneratorFactory
                   && _extension.AddCustomRelationalParameterBasedSqlProcessorFactory == otherSqlServerInfo._extension.AddCustomRelationalParameterBasedSqlProcessorFactory
                   && _extension.AddBulkOperationSupport == otherSqlServerInfo._extension.AddBulkOperationSupport
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
            debugInfo["Thinktecture:TenantDatabaseSupport"] = _extension.AddTenantDatabaseSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TableHintSupport"] = _extension.AddTableHintSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:UseThinktectureSqlServerMigrationsSqlGenerator"] = _extension.UseThinktectureSqlServerMigrationsSqlGenerator.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
