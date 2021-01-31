using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqliteDbContextOptionsExtension : IDbContextOptionsExtension
   {
      private readonly RelationalDbContextOptionsExtension _relationalOptions;

      private SqliteDbContextOptionsExtensionInfo? _info;

      /// <inheritdoc />
      public DbContextOptionsExtensionInfo Info => _info ??= new SqliteDbContextOptionsExtensionInfo(this);

      /// <summary>
      /// Enables and disables support for "RowNumber".
      /// </summary>
      public bool AddRowNumberSupport
      {
         get => _relationalOptions.AddRowNumberSupport;
         set => _relationalOptions.AddRowNumberSupport = value;
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

      /// <summary>
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required for some features.
      /// </summary>
      public bool AddCustomRelationalParameterBasedSqlProcessorFactory
      {
         get => _relationalOptions.AddCustomRelationalParameterBasedSqlProcessorFactory;
         set => _relationalOptions.AddCustomRelationalParameterBasedSqlProcessorFactory = value;
      }

      /// <summary>
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required for some features like 'tenant database support' or for generation of 'DELETE' statements.
      /// </summary>
      public bool AddCustomQuerySqlGeneratorFactory
      {
         get => _relationalOptions.AddCustomQuerySqlGeneratorFactory || AddBulkOperationSupport;
         set => _relationalOptions.AddCustomQuerySqlGeneratorFactory = value;
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
      /// Initializes new instance of <see cref="SqliteDbContextOptionsExtension"/>.
      /// </summary>
      /// <param name="relationalOptions">An instance of <see cref="RelationalDbContextOptionsExtension"/>.</param>
      public SqliteDbContextOptionsExtension(RelationalDbContextOptionsExtension relationalOptions)
      {
         _relationalOptions = relationalOptions ?? throw new ArgumentNullException(nameof(relationalOptions));
      }

      /// <inheritdoc />
      public void ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);

         if (AddCustomQuerySqlGeneratorFactory)
            services.Add<IQuerySqlGeneratorFactory, ThinktectureSqliteQuerySqlGeneratorFactory>(RelationalDbContextOptionsExtension.GetLifetime<IQuerySqlGeneratorFactory>());

         if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
            services.AddSingleton<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory>();

         if (AddCustomRelationalParameterBasedSqlProcessorFactory)
            services.Add<IRelationalParameterBasedSqlProcessorFactory, ThinktectureRelationalParameterBasedSqlProcessorFactory>(GetLifetime<IRelationalParameterBasedSqlProcessorFactory>());

         if (AddTempTableSupport)
         {
            services.TryAddScoped<ITempTableCreator, SqliteTempTableCreator>();
            services.AddTempTableSuffixComponents();
         }

         if (AddBulkOperationSupport)
         {
            services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();
            services.TryAddScoped<SqliteBulkOperationExecutor, SqliteBulkOperationExecutor>();
            services.TryAddScoped<IBulkOperationExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
            services.TryAddScoped<ITempTableBulkOperationExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
         }
      }

      private static ServiceLifetime GetLifetime<TService>()
      {
         return EntityFrameworkRelationalServicesBuilder.RelationalServices[typeof(TService)].Lifetime;
      }

      /// <inheritdoc />
      public void Validate(IDbContextOptions options)
      {
      }

      private class SqliteDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
      {
         private readonly SqliteDbContextOptionsExtension _extension;
         public override bool IsDatabaseProvider => false;

         private string? _logFragment;

         public override string LogFragment => _logFragment ??= $@"
{{
   'Custom QueryableMethodTranslatingExpressionVisitorFactory'={_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory},
   'BulkOperationSupport'={_extension.AddBulkOperationSupport},
   'TempTableSupport'={_extension.AddTempTableSupport}
}}";

         /// <inheritdoc />
         public SqliteDbContextOptionsExtensionInfo(SqliteDbContextOptionsExtension extension)
            : base(extension)
         {
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
         }

         /// <inheritdoc />
         public override long GetServiceProviderHashCode()
         {
            return HashCode.Combine(_extension.AddCustomQuerySqlGeneratorFactory,
                                    _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory,
                                    _extension.AddCustomRelationalParameterBasedSqlProcessorFactory,
                                    _extension.AddBulkOperationSupport,
                                    _extension.AddTempTableSupport);
         }

         /// <inheritdoc />
         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:CustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TempTableSupport"] = _extension.AddTempTableSupport.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
