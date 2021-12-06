namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Getter for a shadow property.
/// </summary>
public interface IShadowPropertyGetter
{
   /// <summary>
   /// Gets the value of the shadow property.
   /// </summary>
   /// <param name="ctx">Context.</param>
   /// <param name="entity">Entity</param>
   /// <returns>The value of the shadow property.</returns>
   object? GetValue(DbContext ctx, object entity);
}
