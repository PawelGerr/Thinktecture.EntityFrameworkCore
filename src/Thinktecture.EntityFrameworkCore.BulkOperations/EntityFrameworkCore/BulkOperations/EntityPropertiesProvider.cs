using System.Linq.Expressions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Obsolete class for backwards compatibility.
/// </summary>
[Obsolete($"This class is obsolete. Please use factory methods defined on '{nameof(IEntityPropertiesProvider)}' instead.", DiagnosticId = "TTEF1000")]
public sealed class EntityPropertiesProvider
{
   /// <summary>
   /// Extracts members from the provided <paramref name="projection"/>.
   /// </summary>
   /// <param name="projection">Projection to extract the members from.</param>
   /// <typeparam name="T">Type of the entity.</typeparam>
   /// <returns>An instance of <see cref="IEntityPropertiesProvider"/> containing members extracted from <paramref name="projection"/>.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">No members couldn't be extracted.</exception>
   /// <exception cref="NotSupportedException">The <paramref name="projection"/> contains unsupported expressions.</exception>
   [Obsolete($"This method is obsolete. Please use '{nameof(IEntityPropertiesProvider)}.{nameof(IEntityPropertiesProvider.Include)}' instead.", DiagnosticId = "TTEF1000")]
   public static IEntityPropertiesProvider From<T>(Expression<Func<T, object?>> projection)
   {
      return IEntityPropertiesProvider.Include(projection);
   }
}
