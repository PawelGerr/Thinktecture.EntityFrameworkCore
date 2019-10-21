using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Cache key factory that takes the schema into account.
   /// </summary>
   public sealed class DefaultSchemaRespectingModelCacheKeyFactory<TFactory> : IModelCacheKeyFactory
      where TFactory : class, IModelCacheKeyFactory
   {
      private readonly TFactory _factory;

      /// <summary>
      /// Initializes new instance of <see cref="DefaultSchemaRespectingModelCacheKeyFactory{TFactory}"/>.
      /// </summary>
      /// <param name="factory">Inner factory.</param>
      public DefaultSchemaRespectingModelCacheKeyFactory(TFactory factory)
      {
         _factory = factory ?? throw new ArgumentNullException(nameof(factory));
      }

      /// <inheritdoc />
      public object Create(DbContext context)
      {
         if (context == null)
            throw new ArgumentNullException(nameof(context));

         var key = _factory.Create(context);
         var schema = context is IDbDefaultSchema dbSchema ? dbSchema.Schema : null;

         // compiler implements Equals and GetHashCode they way we need
         return new { key, schema };
      }
   }
}
