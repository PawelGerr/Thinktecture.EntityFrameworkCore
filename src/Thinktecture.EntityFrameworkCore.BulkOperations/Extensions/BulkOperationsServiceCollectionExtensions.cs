using System;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="IServiceProvider"/>
/// </summary>
public static class BulkOperationsServiceCollectionExtensions
{
   /// <summary>
   /// Registers components required for creation of temp tables.
   /// </summary>
   /// <param name="services">Service collection to register the components with.</param>
   /// <returns>The provided <paramref name="services"/>.</returns>
   public static IServiceCollection AddTempTableSuffixComponents(this IServiceCollection services)
   {
      services.AddScoped<TempTableSuffixLeasing>();
      services.AddSingleton<TempTableSuffixCache>();

      return services;
   }
}