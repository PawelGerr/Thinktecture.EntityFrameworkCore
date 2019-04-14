using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
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
      /// <exception cref="ArgumentNullException"></exception>
      [NotNull]
      public static DbContextOptionsBuilder<T> AddSchemaAwareSqlServerComponents<T>([NotNull] this DbContextOptionsBuilder<T> builder)
         where T : DbContext
      {
         ((DbContextOptionsBuilder)builder).AddSchemaAwareSqlServerComponents();
         return builder;
      }

      /// <summary>
      /// Adds/replaces components that handle with database schema changes at runtime.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"></exception>
      [NotNull]
      public static DbContextOptionsBuilder AddSchemaAwareSqlServerComponents([NotNull] this DbContextOptionsBuilder builder)
      {
         if (builder == null)
            throw new ArgumentNullException(nameof(builder));

         return builder
                .AddSchemaAwareComponents()
                .ReplaceService<IMigrationsSqlGenerator, ThinktectureSqlServerMigrationsSqlGenerator>();
      }
   }
}
