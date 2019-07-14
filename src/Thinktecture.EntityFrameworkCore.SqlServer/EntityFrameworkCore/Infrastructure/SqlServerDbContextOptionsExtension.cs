using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.BulkOperations;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions
   /// </summary>
   public class SqlServerDbContextOptionsExtension : IDbContextOptionsExtension
   {
      /// <inheritdoc />
      [NotNull]
      public string LogFragment => $"{{ 'RowNumberSupport'={AddRowNumberSupport}, 'TempTableSupport'={AddTempTableSupport}, 'UseThinktectureSqlServerMigrationsSqlGenerator'={UseThinktectureSqlServerMigrationsSqlGenerator} }}";

      /// <summary>
      /// Enables and disables support for "RowNumber".
      /// </summary>
      public bool AddRowNumberSupport { get; set; }

      /// <summary>
      /// Enables and disables support for temp tables.
      /// </summary>
      public bool AddTempTableSupport { get; set; }

      /// <summary>
      /// Changes the implementation of <see cref="IMigrationsSqlGenerator"/> to <see cref="ThinktectureSqlServerMigrationsSqlGenerator"/>.
      /// </summary>
      public bool UseThinktectureSqlServerMigrationsSqlGenerator { get; set; }

      /// <inheritdoc />
      public bool ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);
         services.AddSingleton<IMethodCallTranslatorPlugin, SqlServerMethodCallTranslatorPlugin>();

         if (AddTempTableSupport)
         {
            services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();
            services.TryAddSingleton<IPropertiesAccessorGenerator, PropertiesAccessorGenerator>();
            services.TryAddSingleton<ITempTableCreator, SqlServerTempTableCreator>();
            services.TryAddSingleton<ISqlServerBulkOperationExecutor, SqlServerBulkOperationExecutor>();
         }

         if (UseThinktectureSqlServerMigrationsSqlGenerator)
            services.AddTransient<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator>();

         return false;
      }

      /// <inheritdoc />
      public long GetServiceProviderHashCode()
      {
         return 0;
      }

      /// <inheritdoc />
      public void Validate(IDbContextOptions options)
      {
      }
   }
}
