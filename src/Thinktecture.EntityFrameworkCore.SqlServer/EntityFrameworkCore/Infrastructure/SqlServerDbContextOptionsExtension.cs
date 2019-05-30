using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Extensions
   /// </summary>
   public class SqlServerDbContextOptionsExtension : IDbContextOptionsExtension
   {
      /// <inheritdoc />
      [NotNull]
      public string LogFragment => $"{{ 'RowNumberSupport'={AddRowNumberSupport} }}";

      /// <summary>
      /// Enables and disables support for "RowNumber".
      /// </summary>
      public bool AddRowNumberSupport { get; set; }

      /// <inheritdoc />
      public bool ApplyServices(IServiceCollection services)
      {
         services.TryAddSingleton(this);
         services.AddSingleton<IMethodCallTranslatorPlugin, SqlServerMethodCallTranslatorPlugin>();
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
