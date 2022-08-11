using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Disables the model cache.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class CachePerContextModelCacheKeyFactory : IModelCacheKeyFactory
{
   /// <summary>Gets the model cache key for a given context.</summary>
   /// <param name="context">The context to get the model cache key for. </param>
   /// <returns>The created key.</returns>
   public object Create(DbContext context)
   {
      return Create(context, false);
   }

   /// <inheritdoc />
   public object Create(DbContext context, bool designTime)
   {
      return (context, designTime);
   }
}
