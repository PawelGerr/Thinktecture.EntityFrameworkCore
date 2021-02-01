using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqliteDbContextOptionsExtension : DbContextOptionsExtensionBase, IDbContextOptionsExtension
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
      /// The factory is required for some features like for generation of 'DELETE' statements.
      /// </summary>
      public bool AddCustomQuerySqlGeneratorFactory
      {
         get => _addCustomQuerySqlGeneratorFactory || AddBulkOperationSupport;
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

         if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
            AddWithCheck<IQueryableMethodTranslatingExpressionVisitorFactory, ThinktectureSqliteQueryableMethodTranslatingExpressionVisitorFactory, SqliteQueryableMethodTranslatingExpressionVisitorFactory>(services);

         if (AddCustomQuerySqlGeneratorFactory)
            AddWithCheck<IQuerySqlGeneratorFactory, ThinktectureSqliteQuerySqlGeneratorFactory, SqliteQuerySqlGeneratorFactory>(services);

         if (AddCustomRelationalParameterBasedSqlProcessorFactory)
            AddWithCheck<IRelationalParameterBasedSqlProcessorFactory, ThinktectureRelationalParameterBasedSqlProcessorFactory, RelationalParameterBasedSqlProcessorFactory>(services);

         if (AddTempTableSupport)
         {
            services.AddSingleton<TempTableStatementCache<SqliteTempTableCreatorCacheKey>>();
            services.TryAddScoped<ITempTableCreator, SqliteTempTableCreator>();
            services.AddTempTableSuffixComponents();
         }

         if (AddBulkOperationSupport)
         {
            AddEntityDataReader(services);
            services.TryAddScoped<SqliteBulkOperationExecutor, SqliteBulkOperationExecutor>();
            services.TryAddScoped<IBulkOperationExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
            services.TryAddScoped<ITempTableBulkOperationExecutor>(provider => provider.GetRequiredService<SqliteBulkOperationExecutor>());
         }
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
   'Custom QuerySqlGeneratorFactory'={_extension.AddCustomQuerySqlGeneratorFactory},
   'Custom RelationalParameterBasedSqlProcessorFactory'={_extension.AddCustomRelationalParameterBasedSqlProcessorFactory},
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
            return HashCode.Combine(_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory,
                                    _extension.AddCustomQuerySqlGeneratorFactory,
                                    _extension.AddCustomRelationalParameterBasedSqlProcessorFactory,
                                    _extension.AddBulkOperationSupport,
                                    _extension.AddTempTableSupport);
         }

         /// <inheritdoc />
         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:CustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:CustomQuerySqlGeneratorFactory"] = _extension.AddCustomQuerySqlGeneratorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:CustomRelationalParameterBasedSqlProcessorFactory"] = _extension.AddCustomRelationalParameterBasedSqlProcessorFactory.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TempTableSupport"] = _extension.AddTempTableSupport.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
