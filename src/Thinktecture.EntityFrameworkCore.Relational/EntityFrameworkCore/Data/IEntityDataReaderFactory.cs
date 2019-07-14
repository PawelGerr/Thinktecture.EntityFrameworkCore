using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for creation of <see cref="IEntityDataReader"/>.
   /// </summary>
   public interface IEntityDataReaderFactory
   {
      /// <summary>
      /// Creates an <see cref="IEntityDataReader"/> for entities of type <typeparamref name="T"/>.
      /// The data reader reads all properties of the type <typeparamref name="T"/>.
      /// </summary>
      /// <param name="entities">Entities to use by the data reader.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IEntityDataReader"/>.</returns>
      [NotNull]
      IEntityDataReader Create<T>([NotNull] IEnumerable<T> entities)
         where T : class;

      /// <summary>
      /// Creates an <see cref="IEntityDataReader"/> for entities of type <typeparamref name="T"/>.
      /// The data reader reads the provided <paramref name="properties"/> only.
      /// </summary>
      /// <param name="entities">Entities to use by the data reader.</param>
      /// <param name="properties">Properties of the entity of type <typeparamref name="T"/> to generate the data reader for.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>An instance of <see cref="IEntityDataReader"/>.</returns>
      [NotNull]
      IEntityDataReader Create<T>([NotNull] IEnumerable<T> entities, [NotNull] IReadOnlyList<PropertyInfo> properties)
         where T : class;
   }
}
