using System;
using System.Linq;
using System.Reflection;
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
      public static void SetSchema([NotNull] this ModelBuilder modelBuilder, [CanBeNull] string schema, [CanBeNull] Func<IMutableEntityType, bool> predicate = null)
      {
         if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

         var entityTypes = modelBuilder.Model.GetEntityTypes();

         if (predicate != null)
            entityTypes = entityTypes.Where(predicate);

         foreach (var entityType in entityTypes)
         {
            entityType.Relational().Schema = schema;
         }
      }
   }
}
