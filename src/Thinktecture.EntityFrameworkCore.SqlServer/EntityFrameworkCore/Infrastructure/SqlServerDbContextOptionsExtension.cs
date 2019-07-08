using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.Data;
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
      public string LogFragment => $"{{ 'RowNumberSupport'={AddRowNumberSupport}, 'TempTableSupport'={AddTempTableSupport} }}";

      /// <summary>
      /// Enables and disables support for "RowNumber".
      /// </summary>
      public bool AddRowNumberSupport { get; set; }

      /// <summary>
      /// Enables and disables support for temp tables.
      /// </summary>
      public bool AddTempTableSupport { get; set; }

      /// <inheritdoc />
      public bool ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);
         services.AddSingleton<IMethodCallTranslatorPlugin, SqlServerMethodCallTranslatorPlugin>();

         if (AddTempTableSupport)
         {
            services.TryAddSingleton<IEntityDataReaderFactory, EntityDataReaderFactory>();
            services.TryAddSingleton<IEntityDataReaderGenerator, EntityDataReaderGenerator>();
            services.TryAddSingleton<ITempTableCreator, SqlServerTempTableCreator>();
         }

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
