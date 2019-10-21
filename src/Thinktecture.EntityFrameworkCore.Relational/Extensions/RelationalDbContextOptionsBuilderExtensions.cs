using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Query;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="DbContextOptionsBuilder"/>.
   /// </summary>
   public static class RelationalDbContextOptionsBuilderExtensions
   {
      /// <summary>
      /// Add an <see cref="IRelationalTypeMappingSourcePlugin"/> to dependency injection.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <typeparam name="TContext">Type of the context.</typeparam>
      /// <typeparam name="TPlugin">Type of the plugin implementing <see cref="IRelationalTypeMappingSourcePlugin"/>.</typeparam>
      /// <returns>Options builder for chaining.</returns>
      public static DbContextOptionsBuilder<TContext> AddRelationalTypeMappingSourcePlugin<TContext, TPlugin>(this DbContextOptionsBuilder<TContext> builder)
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
      public static DbContextOptionsBuilder AddRelationalTypeMappingSourcePlugin<TPlugin>(this DbContextOptionsBuilder builder)
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
      public static DbContextOptionsBuilder<T> AddSchemaRespectingComponents<T>(this DbContextOptionsBuilder<T> builder,
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
      public static DbContextOptionsBuilder AddSchemaRespectingComponents(this DbContextOptionsBuilder builder,
                                                                          bool addDefaultSchemaRespectingComponents = true)
      {
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.AddSchemaRespectingComponents = addDefaultSchemaRespectingComponents);
         return builder;
      }

      /// <summary>
      /// Adds an <see cref="IEvaluatableExpressionFilterPlugin"/>.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <typeparam name="T">Type of the plugin.</typeparam>
      /// <typeparam name="TPlugin">Type of the plugin.</typeparam>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
      public static DbContextOptionsBuilder<T> AddEvaluatableExpressionFilterPlugin<T, TPlugin>(this DbContextOptionsBuilder<T> builder)
         where T : DbContext
         where TPlugin : IEvaluatableExpressionFilterPlugin
      {
         builder.AddEvaluatableExpressionFilterPlugin<TPlugin>();
         return builder;
      }

      /// <summary>
      /// Adds an <see cref="IEvaluatableExpressionFilterPlugin"/>.
      /// </summary>
      /// <param name="builder">Options builder.</param>
      /// <typeparam name="T">Type of the plugin.</typeparam>
      /// <returns>The provided <paramref name="builder"/>.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <c>null</c>.</exception>
      public static DbContextOptionsBuilder AddEvaluatableExpressionFilterPlugin<T>(this DbContextOptionsBuilder builder)
         where T : IEvaluatableExpressionFilterPlugin
      {
         builder.AddOrUpdateExtension<RelationalDbContextOptionsExtension>(extension => extension.AddEvaluatableExpressionFilterPlugin<T>());
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
      public static void AddOrUpdateExtension<TExtension>(this DbContextOptionsBuilder optionsBuilder,
                                                          Action<TExtension> callback)
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
