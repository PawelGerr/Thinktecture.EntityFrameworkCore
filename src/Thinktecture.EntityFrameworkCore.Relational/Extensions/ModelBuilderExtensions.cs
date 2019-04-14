using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
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
      /// Iterates over entity types and sets the database schema on types matching the <paramref name="predicate"/>.
      /// </summary>
      /// <param name="modelBuilder">Model builder.</param>
      /// <param name="schema">Schema to set.</param>
      /// <param name="predicate">Entity types matching the condition are going to be updated.</param>
      /// <exception cref="ArgumentNullException">
      /// Model builder is <c>null</c>
      /// - or the predicate is <c>null</c>.
      /// </exception>
      public static void SetSchema([NotNull] this ModelBuilder modelBuilder, [CanBeNull] string schema, [CanBeNull] Func<IRelationalEntityTypeAnnotations, bool> predicate = null)
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         var entityTypes = modelBuilder.Model.GetEntityTypes();

         foreach (var entityType in entityTypes)
         {
            var relational = entityType.Relational();

            if (predicate?.Invoke(relational) != false)
               relational.Schema = schema;
         }
      }

      /// <summary>
      /// Introduces and configures a temp table.
      /// </summary>
      /// <param name="modelBuilder">A model builder.</param>
      /// <typeparam name="T">Type of the temp table.</typeparam>
      /// <returns>An entity type builder for further configuration.</returns>
      /// <exception cref="ArgumentNullException"><paramref name="modelBuilder"/> is <c>null</c>.</exception>
      [NotNull]
      public static QueryTypeBuilder<T> ConfigureCustomTempTable<T>([NotNull] this ModelBuilder modelBuilder)
         where T : class
      {
         return modelBuilder.Query<T>().SetTableName();
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
         return modelBuilder.Query<TempTable<TColumn1>>().SetTableName();
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
         return modelBuilder.Query<TempTable<TColumn1, TColumn2>>().SetTableName();
      }

      private static QueryTypeBuilder<T> SetTableName<T>([NotNull] this QueryTypeBuilder<T> builder)
         where T : class
      {
         var tableName = "#" + typeof(T).DisplayName(false);

         return builder.ToView(tableName);
      }
   }
}
