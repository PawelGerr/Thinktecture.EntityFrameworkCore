using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions for DbContextOptions.
   /// </summary>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class SqlServerDbContextOptionsExtension : IDbContextOptionsExtension
   {
      private SqlServerDbContextOptionsExtensionInfo? _info;

      /// <inheritdoc />
      public DbContextOptionsExtensionInfo Info => _info ??= new SqlServerDbContextOptionsExtensionInfo(this);

      /// <summary>
      /// Enables and disables support for "RowNumber".
      /// </summary>
      public bool AddRowNumberSupport { get; set; }

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

      private bool _addCustomQueryableMethodTranslatingExpressionVisitorFactory;

      /// <summary>
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required to be able to translate custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
      /// </summary>
      public bool AddCustomQueryableMethodTranslatingExpressionVisitorFactory
      {
         get => _addCustomQueryableMethodTranslatingExpressionVisitorFactory || AddRowNumberSupport;
         set => _addCustomQueryableMethodTranslatingExpressionVisitorFactory = value;
      }

      /// <summary>
      /// Changes the implementation of <see cref="IMigrationsSqlGenerator"/> to <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
      /// </summary>
      public bool UseThinktectureSqlServerMigrationsSqlGenerator { get; set; }

      /// <inheritdoc />
      public void ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);
         services.Add<IMethodCallTranslatorPlugin, SqlServerMethodCallTranslatorPlugin>(GetLifetime<IMethodCallTranslatorPlugin>());

         if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
            services.AddSingleton<IQueryableMethodTranslatingExpressionVisitorFactory, SqlServerQueryableMethodTranslatingExpressionVisitorFactory>();

         if (AddTempTableSupport)
         {
            var lifetime = GetLifetime<ISqlGenerationHelper>();
            services.TryAdd<ISqlServerTempTableCreator, SqlServerTempTableCreator>(lifetime);
            services.TryAdd(ServiceDescriptor.Describe(typeof(ITempTableCreator), provider => provider.GetRequiredService<ISqlServerTempTableCreator>(), lifetime));
         }

         if (AddBulkOperationSupport)
         {
            services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();
            var lifetime = GetLifetime<ISqlGenerationHelper>();
            services.TryAdd<SqlServerBulkOperationExecutor, SqlServerBulkOperationExecutor>(lifetime);
            services.TryAdd(ServiceDescriptor.Describe(typeof(IBulkOperationExecutor), provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>(), lifetime));
            services.TryAdd(ServiceDescriptor.Describe(typeof(ITempTableBulkOperationExecutor), provider => provider.GetRequiredService<SqlServerBulkOperationExecutor>(), lifetime));
         }

         if (UseThinktectureSqlServerMigrationsSqlGenerator)
            services.Add<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator>(GetLifetime<IMigrationsSqlGenerator>());
      }

      private static ServiceLifetime GetLifetime<TService>()
      {
         return EntityFrameworkRelationalServicesBuilder.RelationalServices[typeof(TService)].Lifetime;
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
   'RowNumberSupport'={_extension.AddRowNumberSupport},
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
            return 0;
         }

         /// <inheritdoc />
         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:RowNumberSupport"] = _extension.AddRowNumberSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TempTableSupport"] = _extension.AddTempTableSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:UseThinktectureSqlServerMigrationsSqlGenerator"] = _extension.UseThinktectureSqlServerMigrationsSqlGenerator.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
