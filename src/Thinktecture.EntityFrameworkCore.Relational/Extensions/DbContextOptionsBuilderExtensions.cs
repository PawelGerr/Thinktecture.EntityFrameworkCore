using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="DbContextOptionsBuilder"/>.
   /// </summary>
   public static class DbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Adds support for "Descending".
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addDescendingSupport">Indication whether to enable or disable the feature.</param>
      /// <typeparam name="TContext">Type of the context.</typeparam>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder<TContext> AddDescendingSupport<TContext>([NotNull] this DbContextOptionsBuilder<TContext> builder,
                                                                                     bool addDescendingSupport = true)
         where TContext : DbContext
      {
         // ReSharper disable once RedundantCast
         ((DbContextOptionsBuilder)builder).AddDescendingSupport(addDescendingSupport);
         return builder;
      }

      /// <summary>
      /// Adds support for "Descending".
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addDescendingSupport">Indication whether to enable or disable the feature.</param>
      /// <returns>Options builder for chaining.</returns>
      [NotNull]
      public static DbContextOptionsBuilder AddDescendingSupport([NotNull] this DbContextOptionsBuilder builder, bool addDescendingSupport = true)
      {
         // ReSharper disable once RedundantCast
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.AddDescendingSupport = addDescendingSupport);
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
      /// Adds/replaces components that respect with database schema changes at runtime.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addDefaultSchemaRespectingComponents">Indication whether to enable or disable the feature.</param>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
      [NotNull]
      public static DbContextOptionsBuilder<T> AddSchemaRespectingComponents<T>([NotNull] this DbContextOptionsBuilder<T> builder,
                                                                           bool addDefaultSchemaRespectingComponents = true)
         where T : DbContext
      {
         ((DbContextOptionsBuilder)builder).AddSchemaRespectingComponents(addDefaultSchemaRespectingComponents);
         return builder;
      }

      /// <summary>
      /// Adds/replaces components that respect database schema changes at runtime.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <param name="addDefaultSchemaRespectingComponents">Indication whether to enable or disable the feature.</param>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
      [NotNull]
      public static DbContextOptionsBuilder AddSchemaRespectingComponents([NotNull] this DbContextOptionsBuilder builder,
                                                                     bool addDefaultSchemaRespectingComponents = true)
      {
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.AddSchemaRespectingComponents = addDefaultSchemaRespectingComponents);
         return builder;
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
