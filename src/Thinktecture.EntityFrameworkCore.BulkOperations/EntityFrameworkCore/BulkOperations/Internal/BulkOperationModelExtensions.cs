using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API.
/// </summary>
public static class BulkOperationModelExtensions
{
   /// <summary>
   /// Gets the entity type for <typeparamref name="T"/>: the regular entity type, or the temp table entity type registered via <c>ConfigureTempTableEntity</c>.
   /// </summary>
   /// <exception cref="EntityTypeNotFoundException">Neither a regular nor a temp table entity type exists for <typeparamref name="T"/>.</exception>
   public static IEntityType GetEntityTypeOrTempTableEntityType<T>(this IModel model)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(model);

      return model.FindEntityType(typeof(T))
             ?? model.FindEntityType(EntityNameProvider.GetTempTableName(typeof(T)))
             ?? throw new EntityTypeNotFoundException(typeof(T));
   }
}
