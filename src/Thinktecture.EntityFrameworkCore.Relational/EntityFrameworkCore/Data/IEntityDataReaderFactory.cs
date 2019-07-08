using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for creation of <see cref="IEntityDataReader"/>.
   /// </summary>
   public interface IEntityDataReaderFactory
   {
      /// <summary>
      /// Creates an <see cref="IEntityDataReader"/> for entities of type <typeparamref name="T"/>.
      /// </summary>
      /// <param name="entities">Entities to use by the data reader.</param>
      /// <param name="entityType">Entity descriptor.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IEntityDataReader"/>.</returns>
      [NotNull]
      IEntityDataReader Create<T>([NotNull] IEnumerable<T> entities, [NotNull] IEntityType entityType)
         where T : class;
   }
}
