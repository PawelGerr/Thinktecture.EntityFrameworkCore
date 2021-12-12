namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Builds and caches property getters.
/// </summary>
public interface IPropertyGetterCache
{
   /// <summary>
   /// Gets a property get for provided <paramref name="property"/>.
   /// </summary>
   /// <param name="property">Property to get the getter for.</param>
   /// <typeparam name="TRootEntity">Type of the root entity.</typeparam>
   /// <returns>Property getter.</returns>
   Func<DbContext, TRootEntity, object?> GetPropertyGetter<TRootEntity>(PropertyWithNavigations property);
}
