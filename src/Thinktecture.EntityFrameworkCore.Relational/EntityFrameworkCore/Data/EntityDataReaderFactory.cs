using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for <see cref="IEntityDataReader"/>.
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
      public IEntityDataReader Create<T>(
         DbContext ctx,
         IEnumerable<T> entities,
         IReadOnlyList<IProperty> properties)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         return new EntityDataReader<T>(ctx, _propertyGetterCache, entities, properties);
      }
   }
}
