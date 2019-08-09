using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Cache key factory that takes the schema into account.
   /// </summary>
   public class DbSchemaAwareModelCacheKeyFactory<TFactory> : IModelCacheKeyFactory
      where TFactory : class, IModelCacheKeyFactory
   {
      private readonly TFactory _factory;

      /// <summary>
      /// Initializes new instance of <see cref="DbSchemaAwareModelCacheKeyFactory{TFactory}"/>.
      /// </summary>
      /// <param name="factory">Inner factory.</param>
      public DbSchemaAwareModelCacheKeyFactory([NotNull] TFactory factory)
      {
         _factory = factory ?? throw new ArgumentNullException(nameof(factory));
      }

      /// <inheritdoc />
      [NotNull]
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
