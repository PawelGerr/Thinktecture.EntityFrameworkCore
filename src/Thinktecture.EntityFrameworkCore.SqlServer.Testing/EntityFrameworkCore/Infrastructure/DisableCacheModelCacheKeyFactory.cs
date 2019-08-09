using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
#pragma warning disable CA1812
   // ReSharper disable once ClassNeverInstantiated.Global
   internal sealed class DisableCacheModelCacheKeyFactory : IModelCacheKeyFactory
   {
      [NotNull]
      public object Create(DbContext context)
      {
         return new object();
      }
   }
}
