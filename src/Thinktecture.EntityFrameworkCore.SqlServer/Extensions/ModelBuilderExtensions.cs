using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extensions for <see cref="ModelBuilder"/>.
   /// </summary>
   public static class ModelBuilderExtensions
   {
      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <typeparam name="T">Type of the temp table.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      [NotNull]
      public static QueryTypeBuilder<T> ConfigureTempTableEntity<T>([NotNull] this ModelBuilder modelBuilder)
         where T : class
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         return modelBuilder.Query<T>().SetTempTableName();
      }

      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <typeparam name="TColumn1">Type of the column.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      [NotNull]
      public static QueryTypeBuilder<TempTable<TColumn1>> ConfigureTempTable<TColumn1>([NotNull] this ModelBuilder modelBuilder)
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         return modelBuilder.Query<TempTable<TColumn1>>().SetTempTableName();
      }

      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
      /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      [NotNull]
      public static QueryTypeBuilder<TempTable<TColumn1, TColumn2>> ConfigureTempTable<TColumn1, TColumn2>([NotNull] this ModelBuilder modelBuilder)
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         return modelBuilder.Query<TempTable<TColumn1, TColumn2>>().SetTempTableName();
      }

      private static QueryTypeBuilder<T> SetTempTableName<T>([NotNull] this QueryTypeBuilder<T> builder)
         where T : class
      {
         var tableName = "#" + typeof(T).DisplayName(false);

         return builder.ToView(tableName);
      }
   }
}
