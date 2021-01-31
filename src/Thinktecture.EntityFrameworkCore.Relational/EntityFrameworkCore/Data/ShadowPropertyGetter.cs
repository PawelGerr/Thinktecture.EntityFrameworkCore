using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Getter for a shadow property.
   /// </summary>
   /// <typeparam name="TValue">Type of the shadow property.</typeparam>
   public sealed class ShadowPropertyGetter<TValue> : IShadowPropertyGetter
   {
      private readonly Func<InternalEntityEntry, TValue> _getter;

      /// <summary>
      /// Initializes new instance of <see cref="ShadowPropertyGetter{TValue}"/>.
      /// </summary>
      /// <param name="getter">Current value accessor of the shadow property.</param>
      /// <exception cref="ArgumentNullException"><paramref name="getter"/> is <c>null</c>.</exception>
      public ShadowPropertyGetter(Delegate getter)
      {
         _getter = (Func<InternalEntityEntry, TValue>)getter ?? throw new ArgumentNullException(nameof(getter));
      }

      /// <inheritdoc />
      [SuppressMessage("ReSharper", "EF1001")]
      public object? GetValue(DbContext context, object entity)
      {
         var entry = context.Entry(entity);
         var internalEntry = ((IInfrastructure<InternalEntityEntry>)entry).Instance;

         return _getter(internalEntry);
      }
   }
}
