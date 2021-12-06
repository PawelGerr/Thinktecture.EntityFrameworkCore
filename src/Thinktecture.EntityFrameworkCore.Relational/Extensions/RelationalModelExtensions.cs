using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

// ReSharper disable once CheckNamespace
namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="IModel"/>.
/// </summary>
public static class RelationalModelExtensions
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
   [SuppressMessage("ReSharper", "EF1001")]
   public static IEntityType GetEntityType(this IModel model, Type type)
   {
      ArgumentNullException.ThrowIfNull(model);
      ArgumentNullException.ThrowIfNull(type);

      var entityType = model.FindEntityType(type);

      if (entityType == null)
         throw new ArgumentException($"The provided type '{type.ShortDisplayName()}' is not part of the provided Entity Framework model.", nameof(type));

      return entityType;
   }
}
