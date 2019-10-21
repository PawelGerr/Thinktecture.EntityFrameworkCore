using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
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
      private SqliteDbContextOptionsExtensionInfo? _info;

      /// <inheritdoc />
      public DbContextOptionsExtensionInfo Info => _info ??= new SqliteDbContextOptionsExtensionInfo(this);

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
      /// A custom factory is registered if <c>true</c>.
      /// The factory is required to be able to translate custom methods like <see cref="RelationalQueryableExtensions.AsSubQuery{TEntity}"/>.
      /// </summary>
      public bool AddCustomQueryableMethodTranslatingExpressionVisitorFactory { get; set; }

      /// <inheritdoc />
      public void ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);

         if (AddCustomQueryableMethodTranslatingExpressionVisitorFactory)
            services.AddSingleton<IQueryableMethodTranslatingExpressionVisitorFactory, SqliteQueryableMethodTranslatingExpressionVisitorFactory>();

         if (AddTempTableSupport)
            services.TryAdd<ITempTableCreator, SqliteTempTableCreator>(GetLifetime<ISqlGenerationHelper>());

         if (AddBulkOperationSupport)
         {
            services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();

            var lifetime = GetLifetime<ISqlGenerationHelper>();
            services.TryAdd<SqliteBulkOperationExecutor, SqliteBulkOperationExecutor>(lifetime);
            services.TryAdd(ServiceDescriptor.Describe(typeof(IBulkOperationExecutor), provider => provider.GetRequiredService<SqliteBulkOperationExecutor>(), lifetime));
            services.TryAdd(ServiceDescriptor.Describe(typeof(ITempTableBulkOperationExecutor), provider => provider.GetRequiredService<SqliteBulkOperationExecutor>(), lifetime));
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
   'BulkOperationSupport'={_extension.AddBulkOperationSupport},
   'TempTableSupport'={_extension.AddTempTableSupport},
   'CustomQueryableMethodTranslatingExpressionVisitorFactory'={_extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory}
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
            return 0;
         }

         /// <inheritdoc />
         public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
         {
            debugInfo["Thinktecture:BulkOperationSupport"] = _extension.AddBulkOperationSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:TempTableSupport"] = _extension.AddTempTableSupport.ToString(CultureInfo.InvariantCulture);
            debugInfo["Thinktecture:CustomQueryableMethodTranslatingExpressionVisitorFactory"] = _extension.AddCustomQueryableMethodTranslatingExpressionVisitorFactory.ToString(CultureInfo.InvariantCulture);
         }
      }
   }
}
