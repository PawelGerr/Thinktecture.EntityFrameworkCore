using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.TempTables;

// ReSharper disable once CheckNamespace
namespace Thinktecture
{
   /// <summary>
   /// Extension methods for <see cref="DbContext"/>.
   /// </summary>
   public static class DbContextExtensions
   {
      /// <summary>
      /// Fetches meta data for entity of provided <paramref name="type"/>.
      /// </summary>
      /// <param name="model">Model of a database context.</param>
      /// <param name="type">Entity type.</param>
      /// <returns>An instance of type <see cref="IEntityType"/>.</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="model"/> is <c>null</c>
      /// - or
      /// <paramref name="type"/> is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="model"/>.</exception>
      [NotNull]
      public static IEntityType GetEntityType([NotNull] this IModel model, [NotNull] Type type)
      {
         if (model == null)
            throw new ArgumentNullException(nameof(model));
         if (type == null)
            throw new ArgumentNullException(nameof(type));

         var entityType = model.FindEntityType(type);

         if (entityType == null)
            throw new ArgumentException($"The provided type '{type.DisplayName()}' is not part of the provided Entity Framework model.", nameof(type));

         return entityType;
      }

      /// <summary>
      /// Creates a temp table using custom type '<typeparamref name="T"/>'.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="makeTableNameUnique">Indication whether the table name should be unique.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <typeparam name="T">Type of custom temp table.</typeparam>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException"><paramref name="ctx"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">The provided type <typeparamref name="T"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      public static Task<string> CreateTempTableAsync<T>([NotNull] this DbContext ctx, bool makeTableNameUnique = false, CancellationToken cancellationToken = default)
         where T : class
      {
         return ctx.CreateTempTableAsync(typeof(T), new TempTableCreationOptions { MakeTableNameUnique = makeTableNameUnique }, cancellationToken);
      }

      /// <summary>
      /// Creates a temp table.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="type">Type of the entity.</param>
      /// <param name="options">Options.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      /// <returns>Table name</returns>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="ctx"/> is <c>null</c>
      /// - or
      /// <paramref name="type"/> is <c>null</c>.
      /// </exception>
      /// <exception cref="ArgumentException">The provided type <paramref name="type"/> is not known by provided <paramref name="ctx"/>.</exception>
      [NotNull, ItemNotNull]
      public static Task<string> CreateTempTableAsync([NotNull] this DbContext ctx, [NotNull] Type type, [NotNull] TempTableCreationOptions options, CancellationToken cancellationToken)
      {
         var entityType = ctx.Model.GetEntityType(type);
         return ctx.GetService<ITempTableCreator>().CreateTempTableAsync(ctx, entityType, options, cancellationToken);
      }
   }
}
