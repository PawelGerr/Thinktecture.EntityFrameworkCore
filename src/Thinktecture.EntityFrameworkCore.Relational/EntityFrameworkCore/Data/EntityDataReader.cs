using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Data reader for Entity Framework Core entities.
   /// </summary>
   /// <typeparam name="T">Type of the entity.</typeparam>
   public class EntityDataReader<T> : EntityDataReaderBase<T>
      where T : class
   {
      private readonly IReadOnlyList<PropertyInfo> _getValuePropertyInfos;
      private readonly Func<T, int, object> _getValue;

      /// <summary>
      /// Initializes new instance of <see cref="EntityDataReader{T}"/>.
      /// </summary>
      /// <param name="entities">Entities to iterate over.</param>
      /// <param name="propertiesToRead">Properties to read.</param>
      /// <param name="getValuePropertyInfos">Properties of the entity used to generated the delegate <paramref name="getValue"/>.</param>
      /// <param name="getValue">
      /// Callback for getting a value of the property with a specific index.
      /// The index must match with the provided <paramref name="getValuePropertyInfos"/>.
      /// </param>
      public EntityDataReader([NotNull] IEnumerable<T> entities,
                              [NotNull] IReadOnlyList<PropertyInfo> propertiesToRead,
                              [NotNull] IReadOnlyList<PropertyInfo> getValuePropertyInfos,
                              [NotNull] Func<T, int, object> getValue)
         : base(entities, propertiesToRead)
      {
         _getValuePropertyInfos = getValuePropertyInfos ?? throw new ArgumentNullException(nameof(getValuePropertyInfos));
         _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
      }

      /// <inheritdoc />
      public override int GetPropertyIndex([NotNull] PropertyInfo propertyInfo)
      {
         if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

         var index = _getValuePropertyInfos.IndexOf(propertyInfo);

         if (index >= 0)
            return index;

         throw new ArgumentException($"The property '{propertyInfo.Name}' of type '{propertyInfo.PropertyType.DisplayName()}' is not a member of type '{typeof(T).DisplayName()}'.");
      }

      /// <inheritdoc />
      public override object GetValue(int i)
      {
         return _getValue(Current, i);
      }
   }
}
