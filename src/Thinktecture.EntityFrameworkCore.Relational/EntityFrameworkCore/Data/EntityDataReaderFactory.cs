using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore.Data
{
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
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         return new EntityDataReader<T>(ctx, _propertyGetterCache, entities, properties, ensureReadEntitiesCollection);
      }
   }
}
