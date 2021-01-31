using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Builds and caches property getters.
   /// </summary>
   public interface IPropertyGetterCache
   {
      /// <summary>
      /// Gets a property get for provided <paramref name="property"/>.
      /// </summary>
      /// <param name="property">Property to get the getter for.</param>
      /// <typeparam name="TEntity">Type of the entity the <paramref name="property"/> belongs to.</typeparam>
      /// <returns>Property getter.</returns>
      Func<DbContext, TEntity, object?> GetPropertyGetter<TEntity>(IProperty property)
         where TEntity : class;
   }
}
