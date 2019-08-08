using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Getter for a shadow property.
   /// </summary>
   /// <typeparam name="TValue">Type of the shadow property.</typeparam>
   public class ShadowPropertyGetter<TValue> : IShadowPropertyGetter
   {
      private readonly DbContext _ctx;
      private readonly Func<InternalEntityEntry, TValue> _getter;

      /// <summary>
      /// Initializes new instance of <see cref="ShadowPropertyGetter{TValue}"/>.
      /// </summary>
      /// <param name="ctx">Database context.</param>
      /// <param name="getter">Current value accessor of the shadow property.</param>
      /// <exception cref="ArgumentNullException">
      /// <paramref name="ctx"/> is <c>null</c>
      /// - or
      /// <paramref name="getter"/> is <c>null</c>.
      /// </exception>
      public ShadowPropertyGetter([NotNull] DbContext ctx, [NotNull] Delegate getter)
      {
         if (getter == null)
            throw new ArgumentNullException(nameof(getter));

         _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
         _getter = (Func<InternalEntityEntry, TValue>)getter;
      }

      /// <inheritdoc />
      public object GetValue(object entity)
      {
         var entry = _ctx.Entry(entity);
         var internalEntry = ((IInfrastructure<InternalEntityEntry>)entry).Instance;

         return _getter(internalEntry);
      }
   }
}
