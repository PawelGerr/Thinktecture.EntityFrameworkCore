using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for <see cref="IEntityDataReader"/>.
   /// </summary>
   // ReSharper disable once ClassNeverInstantiated.Global
   public class EntityDataReaderFactory : IEntityDataReaderFactory
   {
      /// <inheritdoc />
      public IEntityDataReader Create<T>(IEnumerable<T> entities, IReadOnlyList<IProperty> properties)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         return new EntityDataReader<T>(entities, properties);
      }
   }
}
