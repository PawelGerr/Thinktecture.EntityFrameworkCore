using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class DbContextExtensions
   {
      /// <summary>
      /// Get table schema and name
      /// </summary>
      /// <param name="ctx">An instance of <see cref="DbContext"/> the provided entity type belongs to.</param>
      /// <param name="type">Entity type to fetch the table schema and name for.</param>
      /// <returns>Table schema and table name for provided entity type.</returns>
      /// <exception cref="ArgumentNullException"></exception>
      public static (string Schema, string TableName) GetTableIdentifier([NotNull] this DbContext ctx, [NotNull] Type type)
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var entityType = ctx.Model.FindEntityType(type);

         if (entityType == null)
            throw new ArgumentException($"The provided type '{type.DisplayName()}' is not known by the database context '{ctx.GetType().DisplayName()}'.", nameof(type));

         var relational = entityType.Relational();
         return (relational.Schema, relational.TableName);
      }
   }
}
