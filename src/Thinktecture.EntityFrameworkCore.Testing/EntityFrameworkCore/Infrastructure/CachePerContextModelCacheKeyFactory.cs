using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Disables the model cache.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class CachePerContextModelCacheKeyFactory : IModelCacheKeyFactory
{
   /// <inheritdoc />
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