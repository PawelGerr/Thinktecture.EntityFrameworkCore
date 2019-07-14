using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Generates code required by the <see cref="EntityDataReader{T}"/>
   /// </summary>
   public interface IPropertiesAccessorGenerator
   {
      /// <summary>
      /// Create property accessor.
      /// </summary>
      /// <param name="properties">Properties of type <typeparamref name="T"/>.</param>
      /// <typeparam name="T">Type of the entity.</typeparam>
      /// <returns>Properties accessor.</returns>
      [NotNull]
      Func<T, int, object> CreatePropertiesAccessor<T>([NotNull] IReadOnlyList<PropertyInfo> properties);
   }
}
