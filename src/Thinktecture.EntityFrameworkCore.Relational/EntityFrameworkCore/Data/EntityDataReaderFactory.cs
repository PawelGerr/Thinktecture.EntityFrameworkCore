namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Factory for <see cref="IEntityDataReader{T}"/>.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class EntityDataReaderFactory : IEntityDataReaderFactory
{
   private readonly IPropertyGetterCache _propertyGetterCache;

   /// <summary>
   /// Initializes new instance of <see cref="EntityDataReaderFactory"/>
   /// </summary>
   /// <param name="propertyGetterCache">Property getter cache.</param>
   public EntityDataReaderFactory(IPropertyGetterCache propertyGetterCache)
   {
      _propertyGetterCache = propertyGetterCache ?? throw new ArgumentNullException(nameof(propertyGetterCache));
   }

   /// <inheritdoc />
   public IEntityDataReader<T> Create<T>(
      DbContext ctx,
      IEnumerable<T> entities,
      IReadOnlyList<PropertyWithNavigations> properties,
      bool ensureReadEntitiesCollection)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(ctx);
      ArgumentNullException.ThrowIfNull(entities);
      ArgumentNullException.ThrowIfNull(properties);

      return new EntityDataReader<T>(ctx, _propertyGetterCache, entities, properties, ensureReadEntitiesCollection);
   }
}
