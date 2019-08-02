using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="DbContextOptionsBuilder"/>.
   /// </summary>
   public static class DbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Adds custom implementation of <see cref="IExpressionFragmentTranslator"/>.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="translator">Translator to add.</param>
      /// <typeparam name="TContext">Type of the context.</typeparam>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder<TContext> AddExpressionFragmentTranslator<TContext>([NotNull] this DbContextOptionsBuilder<TContext> builder,
                                                                                                [NotNull] IExpressionFragmentTranslator translator)
         where TContext : DbContext
      {
         // ReSharper disable once RedundantCast
         ((DbContextOptionsBuilder)builder).AddExpressionFragmentTranslator(translator);
         return builder;
      }

      /// <summary>
      /// Adds custom implementation of <see cref="IExpressionFragmentTranslator"/>.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="translator">Translator to add.</param>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder AddExpressionFragmentTranslator([NotNull] this DbContextOptionsBuilder builder, [NotNull] IExpressionFragmentTranslator translator)
      {
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.AddExpressionFragmentTranslator(translator));
         return builder;
      }

      /// <summary>
      /// Add an <see cref="IRelationalTypeMappingSourcePlugin"/> to dependency injection.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <typeparam name="TContext">Type of the context.</typeparam>
      /// <typeparam name="TPlugin">Type of the plugin implementing <see cref="IRelationalTypeMappingSourcePlugin"/>.</typeparam>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder<TContext> AddRelationalTypeMappingSourcePlugin<TContext, TPlugin>([NotNull] this DbContextOptionsBuilder<TContext> builder)
         where TContext : DbContext
         where TPlugin : IRelationalTypeMappingSourcePlugin
      {
         // ReSharper disable once RedundantCast
         ((DbContextOptionsBuilder)builder).AddRelationalTypeMappingSourcePlugin<TPlugin>();
         return builder;
      }

      /// <summary>
      /// Add an <see cref="IRelationalTypeMappingSourcePlugin"/> to dependency injection.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <typeparam name="TPlugin">Type of the plugin implementing <see cref="IRelationalTypeMappingSourcePlugin"/>.</typeparam>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder AddRelationalTypeMappingSourcePlugin<TPlugin>([NotNull] this DbContextOptionsBuilder builder)
         where TPlugin : IRelationalTypeMappingSourcePlugin
      {
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.AddRelationalTypeMappingSourcePlugin(typeof(TPlugin)));
         return builder;
      }

      /// <summary>
      /// Enables the support for <see cref="IExpressionFragmentTranslatorPlugin"/>.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder AddExpressionFragmentTranslatorPluginSupport([NotNull] this DbContextOptionsBuilder builder)
      {
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.ExpressionFragmentTranslatorPluginSupport = true);
         return builder;
      }

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

      /// <summary>
      /// Adds or updates an extension of type <typeparamref name="TExtension"/>.
      /// </summary>
      /// <param name="optionsBuilder">Options builder.</param>
      /// <param name="callback">Callback that updates the extension.</param>
      /// <typeparam name="TExtension">Type of the extension.</typeparam>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="optionsBuilder"/> is null
      /// - or
      /// <paramref name="callback"/> is null.
      /// </exception>
      public static void AddOrUpdateExtension<TExtension>([NotNull] this DbContextOptionsBuilder optionsBuilder,
                                                          [NotNull] Action<TExtension> callback)
         where TExtension : class, IDbContextOptionsExtension, new()
      {
         if (optionsBuilder == null)
            throw new ArgumentNullException(nameof(optionsBuilder));
         if (callback == null)
            throw new ArgumentNullException(nameof(callback));

         var extension = optionsBuilder.Options.FindExtension<TExtension>() ?? new TExtension();

         callback(extension);

         var builder = (IDbContextOptionsBuilderInfrastructure)optionsBuilder;
         builder.AddOrUpdateExtension(extension);
      }
   }
}
