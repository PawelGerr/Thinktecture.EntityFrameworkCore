using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore;

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
   public static IEntityType GetEntityType(this IModel model, Type type)
   {
      ArgumentNullException.ThrowIfNull(model);
      ArgumentNullException.ThrowIfNull(type);

      var entityType = model.FindEntityType(type);

      if (entityType == null)
         throw new EntityTypeNotFoundException(type);

      return entityType;
   }

   /// <summary>
   /// Fetches meta data for entity of provided <paramref name="name"/>.
   /// </summary>
   /// <param name="model">Model of a database context.</param>
   /// <param name="name">Entity name.</param>
   /// <param name="fallbackEntityType">Type of the entity to search for. This type is used only then if the search with the <paramref name="name"/> wasn't successful.</param>
   /// <returns>An instance of type <see cref="IEntityType"/>.</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="model"/> is <c>null</c>
   /// - or
   /// <paramref name="name"/> is <c>null</c>.
   /// </exception>
   /// <exception cref="ArgumentException">The provided type <paramref name="name"/> is not known by provided <paramref name="model"/>.</exception>
   public static IEntityType GetEntityType(this IModel model, string name, Type? fallbackEntityType = null)
   {
      ArgumentNullException.ThrowIfNull(model);
      ArgumentNullException.ThrowIfNull(name);

      var entityType = model.FindEntityType(name);

      if (entityType is null && fallbackEntityType is not null)
         entityType = model.FindEntityType(fallbackEntityType);

      if (entityType is not null)
         return entityType;

      if (fallbackEntityType is not null)
         throw new EntityTypeNotFoundException(name, fallbackEntityType);

      throw new EntityTypeNotFoundException(name);
   }
}
