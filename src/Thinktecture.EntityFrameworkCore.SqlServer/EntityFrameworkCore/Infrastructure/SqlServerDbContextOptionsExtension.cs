using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqlServerDbContextOptionsExtension : IDbContextOptionsExtension
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
      /// Enables and disables support for tenant support.
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
         get => _addCustomQueryableMethodTranslatingExpressionVisitorFactory || AddBulkOperationSupport || AddRowNumberSupport;
         set => _addCustomQueryableMethodTranslatingExpressionVisitorFactory = value;
      }

      private bool _addCustomRelationalParameterBasedSqlProcessorFactory;

      /// <summary>
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required for some features.
      /// </summary>
      public bool AddCustomRelationalParameterBasedSqlProcessorFactory
      {
         get => _addCustomRelationalParameterBasedSqlProcessorFactory || AddBulkOperationSupport || AddRowNumberSupport;
         set => _addCustomRelationalParameterBasedSqlProcessorFactory = value;
      }

      private bool _addCustomQuerySqlGeneratorFactory;

      /// <summary>
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required for some features like 'tenant database support' or for generation of 'DELETE' statements.
      /// </summary>
      public bool AddCustomQuerySqlGeneratorFactory
      {
         get => _addCustomQuerySqlGeneratorFactory || AddBulkOperationSupport || AddTenantDatabaseSupport;
         set => _addCustomQuerySqlGeneratorFactory = value;
      }

      /// <summary>
      /// Enables and disables support for temp tables.
      /// </summary>
      public bool AddTempTableSupport { get; set; }

      private bool _addBulkOperationSupport;

      /// <summary>
      /// Enables and disables support for bulk operations.
      /// </summary>
      public bool AddBulkOperationSupport
      {
         get => _addBulkOperationSupport || AddTempTableSupport; // temp tables require bulk operations
         set => _addBulkOperationSupport = value;
      }

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
            services.AddSingleton<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureSqlServerQueryableMethodTranslatingExpressionVisitorFactory>();

         if (AddCustomQuerySqlGeneratorFactory)
            services.Add<IQuerySqlGeneratorFactory, ThinktectureSqlServerQuerySqlGeneratorFactory>(RelationalDbContextOptionsExtension.GetLifetime<IQuerySqlGeneratorFactory>());

         if (AddCustomRelationalParameterBasedSqlProcessorFactory)
            services.Add<IRelationalParameterBasedSqlProcessorFactory, ThinktectureSqlServerParameterBasedSqlProcessorFactory>(RelationalDbContextOptionsExtension.GetLifetime<IRelationalParameterBasedSqlProcessorFactory>());

         if (AddTempTableSupport)
         {
            services.TryAddScoped<ISqlServerTempTableCreator, SqlServerTempTableCreator>();
            services.TryAddScoped<ITempTableCreator>(provider => provider.GetRequiredService<ISqlServerTempTableCreator>());
            services.AddTempTableSuffixComponents();
         }

         if (AddBulkOperationSupport)
         {
            services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();
            services.TryAddScoped<SqlServerBulkOperationExecutor, SqlServerBulkOperationExecutor>();
            services.TryAddScoped<IBulkOperationExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
            services.TryAddScoped<ITempTableBulkOperationExecutor>(provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>());
         }

         if (UseThinktectureSqlServerMigrationsSqlGenerator)
            services.Add<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator>(RelationalDbContextOptionsExtension.GetLifetime<IMigrationsSqlGenerator>());
      }

      /// <summary>
      /// Adds a service descriptor for registration of custom services with internal dependency injection container of Entity Framework Core.
      /// </summary>
      /// <param name="serviceDescriptor">Service descriptor to add.</param>
      /// <exception cref="ArgumentNullException"><paramref name="serviceDescriptor"/> is <c>null</c>.</exception>
      public void Add(ServiceDescriptor serviceDescriptor)
      {
         _relationalOptions.Add(serviceDescriptor);
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
   'TempTableSupport'={_extension.AddTempTableSupport},
   'UseThinktectureSqlServerMigrationsSqlGenerator'={_extension.UseThinktectureSqlServerMigrationsSqlGenerator}
}}";

         /// <inheritdoc />
         public SqlServerDbContextOptionsExtensionInfo(SqlServerDbContextOptionsExtension extension)
            : base(extension)
         {
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
         }

         /// <inheritdoc />
         public override long GetServiceProviderHashCode()
         {
            return HashCode.Combine(_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory,
                                    _extension.AddCustomQuerySqlGeneratorFactory,
                                    _extension.AddCustomRelationalParameterBasedSqlProcessorFactory,
                                    _extension.AddBulkOperationSupport,
                                    _extension.AddTempTableSupport,
                                    _extension.UseThinktectureSqlServerMigrationsSqlGenerator);
         }

         /// <inheritdoc />
         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:CustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:CustomQuerySqlGeneratorFactory"] = _extension.AddCustomQuerySqlGeneratorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:CustomRelationalParameterBasedSqlProcessorFactory"] = _extension.AddCustomRelationalParameterBasedSqlProcessorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TempTableSupport"] = _extension.AddTempTableSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:UseThinktectureSqlServerMigrationsSqlGenerator"] = _extension.UseThinktectureSqlServerMigrationsSqlGenerator.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
