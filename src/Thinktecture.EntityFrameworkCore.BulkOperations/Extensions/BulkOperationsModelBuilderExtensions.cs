using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="ModelBuilder"/>.
   /// </summary>
   public static class BulkOperationsModelBuilderExtensions
   {
      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <param name="isKeyless">Indication whether the entity has a key or not.</param>
      /// <typeparam name="T">Type of the temp table.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      public static EntityTypeBuilder<T> ConfigureTempTableEntity<T>(this ModelBuilder modelBuilder, bool isKeyless = true)
         where T : class
      {
         return modelBuilder.Configure<T>(isKeyless);
      }

      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <param name="isKeyless">Indication whether the entity has a key or not.</param>
      /// <typeparam name="TColumn1">Type of the column.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      public static EntityTypeBuilder<TempTable<TColumn1>> ConfigureTempTable<TColumn1>(this ModelBuilder modelBuilder, bool isKeyless = true)
      {
         var builder = modelBuilder.Configure<TempTable<TColumn1>>(isKeyless);

         if (!isKeyless)
         {
            builder.HasKey(t => t.Column1);
            builder.Property(t => t.Column1).ValueGeneratedNever();
         }

         return builder;
      }

      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <param name="isKeyless">Indication whether the entity has a key or not.</param>
      /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
      /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      public static EntityTypeBuilder<TempTable<TColumn1, TColumn2>> ConfigureTempTable<TColumn1, TColumn2>(this ModelBuilder modelBuilder, bool isKeyless = true)
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         var builder = modelBuilder.Configure<TempTable<TColumn1, TColumn2>>(isKeyless);

         if (!isKeyless)
         {
            builder.HasKey(t => new { t.Column1, t.Column2 });
            builder.Property(t => t.Column1).ValueGeneratedNever();
            builder.Property(t => t.Column2).ValueGeneratedNever();
         }

         return builder;
      }

      [SuppressMessage("ReSharper", "EF1001")]
      private static EntityTypeBuilder<T> Configure<T>(this ModelBuilder modelBuilder, bool isKeyless)
         where T : class
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         var tableName = "#" + typeof(T).ShortDisplayName();

         var builder = modelBuilder.Entity<T>().ToTable(tableName);

         if (isKeyless)
            builder.HasNoKey();

         return builder;
      }
   }
}
