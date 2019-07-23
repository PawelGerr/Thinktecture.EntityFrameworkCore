using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="DbContextOptionsBuilder"/>.
   /// </summary>
   public static class DbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Adds/replaces components that handle with database schema changes at runtime.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
      [NotNull]
      public static DbContextOptionsBuilder<T> AddSchemaAwareComponents<T>([NotNull] this DbContextOptionsBuilder<T> builder)
         where T : DbContext
      {
         ((DbContextOptionsBuilder)builder).AddSchemaAwareComponents();
         return builder;
      }

      /// <summary>
      /// Adds/replaces components that handle with database schema changes at runtime.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
      [NotNull]
      public static DbContextOptionsBuilder AddSchemaAwareComponents([NotNull] this DbContextOptionsBuilder builder)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         return builder.ReplaceService<IModelCacheKeyFactory, DbSchemaAwareModelCacheKeyFactory>()
                       .ReplaceService<IModelCustomizer, DbSchemaAwareModelCustomizer>()
                       .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
      }
   }
}
