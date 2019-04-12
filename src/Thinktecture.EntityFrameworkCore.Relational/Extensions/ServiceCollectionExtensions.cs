using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="IServiceCollection"/>.
   /// </summary>
   public static class ServiceCollectionExtensions
   {
      /// <summary>
      /// Adds a database context to dependency injection and registers additional components
      /// that handle with database schema changes at runtime.
      /// </summary>
      /// <param name="services">Service collection.</param>
      /// <param name="optionsAction">Allows further configuration of the database context.</param>
      /// <typeparam name="T">Type of the database context.</typeparam>
      /// <returns>Provided <see cref="services"/>.</returns>
      /// <exception cref="ArgumentNullException">Service collection is <c>null</c>.</exception>
      public static IServiceCollection AddSchemaAwareDbContext<T>([NotNull] this IServiceCollection services,
                                                                  [CanBeNull] Action<DbContextOptionsBuilder> optionsAction = null)
         where T : DbContext, IDbContextSchema
      {
         if (services == null)
            throw new ArgumentNullException(nameof(services));

         return services.AddDbContext<T>(builder =>
                                         {
                                            builder.ReplaceService<IModelCacheKeyFactory, DbSchemaAwareModelCacheKeyFactory>()
                                                   .ReplaceService<IModelCustomizer, DbSchemaAwareModelCustomizer>()
                                                   .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();

                                            optionsAction?.Invoke(builder);
                                         });
      }
   }
}
