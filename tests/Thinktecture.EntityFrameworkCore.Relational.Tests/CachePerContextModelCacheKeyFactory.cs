using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture
{
   // ReSharper disable once ClassNeverInstantiated.Global
   public class CachePerContextModelCacheKeyFactory : IModelCacheKeyFactory
   {
      /// <inheritdoc />
      [NotNull]
      public object Create(DbContext context)
      {
         return context;
      }
   }
}
