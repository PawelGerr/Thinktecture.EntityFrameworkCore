using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

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
      ArgumentNullException.ThrowIfNull(context);

      var key = _factory.Create(context, designTime);
      // ReSharper disable once SuspiciousTypeConversion.Global
      var schema = context is IDbDefaultSchema dbSchema ? dbSchema.Schema : null;

      // compiler implements Equals and GetHashCode they way we need
      return new { key, schema, designTime };
   }
}
