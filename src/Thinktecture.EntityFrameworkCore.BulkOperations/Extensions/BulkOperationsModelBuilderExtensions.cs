using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thinktecture.EntityFrameworkCore.Parameters;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class BulkOperationsModelBuilderExtensions
{
   /// <summary>
   /// Introduces and configures a scalar parameter.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="T">Type of the column.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureScalarCollectionParameter<T>(
      this ModelBuilder modelBuilder,
      Action<EntityTypeBuilder<ScalarCollectionParameter<T>>>? buildAction = null)
   {
      var builder = modelBuilder.SharedTypeEntity<ScalarCollectionParameter<T>>(EntityNameProvider.GetCollectionParameterName(typeof(T), true))
                                .ToTable(typeof(ScalarCollectionParameter<T>).ShortDisplayName(),
                                         static tableBuilder => tableBuilder.ExcludeFromMigrations())
                                .HasNoKey();

      buildAction?.Invoke(builder);
   }

   /// <summary>
   /// Introduces and configures a complex parameter.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="T">Type of the parameter.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureComplexCollectionParameter<T>(
      this ModelBuilder modelBuilder,
      Action<EntityTypeBuilder<T>>? buildAction = null)
      where T : class
   {
      var builder = modelBuilder.SharedTypeEntity<T>(EntityNameProvider.GetCollectionParameterName(typeof(T), false));

      builder.ToTable(typeof(T).ShortDisplayName(),
                      static tableBuilder => tableBuilder.ExcludeFromMigrations())
             .HasNoKey();

      buildAction?.Invoke(builder);
   }

   /// <summary>
   /// Introduces and configures a keyless temp table.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="T">Type of the temp table.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureTempTableEntity<T>(
      this ModelBuilder modelBuilder,
      Action<EntityTypeBuilder<T>>? buildAction)
      where T : class
   {
      ConfigureTempTableEntity(modelBuilder, true, buildAction);
   }

   /// <summary>
   /// Introduces and configures a temp table.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="isKeyless">Indication whether the entity has a key or not.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="T">Type of the temp table.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureTempTableEntity<T>(
      this ModelBuilder modelBuilder,
      bool isKeyless = true,
      Action<EntityTypeBuilder<T>>? buildAction = null)
      where T : class
   {
      var builder = modelBuilder.ConfigureTempTableInternal<T>(isKeyless);

      buildAction?.Invoke(builder);
   }

   /// <summary>
   /// Introduces and configures a keyless temp table.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="TColumn1">Type of the column.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureTempTable<TColumn1>(
      this ModelBuilder modelBuilder,
      Action<EntityTypeBuilder<TempTable<TColumn1>>>? buildAction)
   {
      ConfigureTempTable(modelBuilder, true, buildAction);
   }

   /// <summary>
   /// Introduces and configures a temp table.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="isKeyless">Indication whether the entity has a key or not.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="TColumn1">Type of the column.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureTempTable<TColumn1>(
      this ModelBuilder modelBuilder,
      bool isKeyless = true,
      Action<EntityTypeBuilder<TempTable<TColumn1>>>? buildAction = null)
   {
      var builder = modelBuilder.ConfigureTempTableInternal<TempTable<TColumn1>>(isKeyless);

      if (!isKeyless)
      {
         builder.HasKey(t => t.Column1);
         builder.Property(t => t.Column1).ValueGeneratedNever();
      }

      buildAction?.Invoke(builder);
   }

   /// <summary>
   /// Introduces and configures a keyless temp table.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureTempTable<TColumn1, TColumn2>(
      this ModelBuilder modelBuilder,
      Action<EntityTypeBuilder<TempTable<TColumn1, TColumn2>>>? buildAction)
   {
      ConfigureTempTable(modelBuilder, true, buildAction);
   }

   /// <summary>
   /// Introduces and configures a temp table.
   /// </summary>
   /// <param name="modelBuilder">A model builder.</param>
   /// <param name="isKeyless">Indication whether the entity has a key or not.</param>
   /// <param name="buildAction">An action that performs configuration of the entity type.</param>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   /// <returns>An entity type builder for further configuration.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
   public static void ConfigureTempTable<TColumn1, TColumn2>(
      this ModelBuilder modelBuilder,
      bool isKeyless = true,
      Action<EntityTypeBuilder<TempTable<TColumn1, TColumn2>>>? buildAction = null)
   {
      ArgumentNullException.ThrowIfNull(modelBuilder);

      var builder = modelBuilder.ConfigureTempTableInternal<TempTable<TColumn1, TColumn2>>(isKeyless);

      if (!isKeyless)
      {
         builder.HasKey(t => new { t.Column1, t.Column2 });
         builder.Property(t => t.Column1).ValueGeneratedNever();
         builder.Property(t => t.Column2).ValueGeneratedNever();
      }

      buildAction?.Invoke(builder);
   }

   private static EntityTypeBuilder<T> ConfigureTempTableInternal<T>(this ModelBuilder modelBuilder, bool isKeyless)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(modelBuilder);

      var builder = modelBuilder.SharedTypeEntity<T>(EntityNameProvider.GetTempTableName(typeof(T)))
                                .ToTable($"#{typeof(T).ShortDisplayName()}",
                                         tableBuilder => tableBuilder.ExcludeFromMigrations());

      if (isKeyless)
         builder.HasNoKey();

      return builder;
   }
}
