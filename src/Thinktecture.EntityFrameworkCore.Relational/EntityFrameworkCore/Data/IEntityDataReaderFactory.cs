using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for creation of <see cref="IEntityDataReader{T}"/>.
   /// </summary>
   public interface IEntityDataReaderFactory
   {
      /// <summary>
      /// Creates an <see cref="IEntityDataReader{T}"/> for entities of type <typeparamref name="T"/>.
      /// The data reader reads the provided <paramref name="properties"/> only.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to use by the data reader.</param>
      /// <param name="properties">Properties of the entity of type <typeparamref name="T"/> to generate the data reader for.</param>
      /// <param name="ensureReadEntitiesCollection">Makes sure the method <see cref="IEntityDataReader{T}.GetReadEntities"/> has a collection to return.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IEntityDataReader{T}"/>.</returns>
      IEntityDataReader<T> Create<T>(
         DbContext ctx,
         IEnumerable<T> entities,
         IReadOnlyList<PropertyWithNavigations> properties,
         bool ensureReadEntitiesCollection)
         where T : class;
   }
}
