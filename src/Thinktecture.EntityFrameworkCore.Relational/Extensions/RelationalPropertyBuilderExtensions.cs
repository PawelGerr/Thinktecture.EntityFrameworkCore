using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thinktecture;

/// <summary>
/// Extension methods for <see cref="PropertyBuilder{TProperty}"/>.
/// </summary>
public static class RelationalPropertyBuilderExtensions
{
   /// <summary>
   /// Changes the <see cref="ValueComparer"/> so that values of type <typeparamref name="T"/> are compared using reference equality.
   /// </summary>
   /// <param name="propertyBuilder">The builder of property to set the <see cref="ValueComparer"/> for.</param>
   /// <typeparam name="T">The type of the property.</typeparam>
   /// <returns>The provided <paramref name="propertyBuilder"/> for chaining.</returns>
   public static PropertyBuilder<T> UseReferenceEqualityComparer<T>(this PropertyBuilder<T> propertyBuilder)
      where T : class
   {
      propertyBuilder.Metadata
                     .SetValueComparer(new ValueComparer<T>((obj, otherObj) => ReferenceEquals(obj, otherObj),
                                                            obj => RuntimeHelpers.GetHashCode(obj),
                                                            obj => obj));

      return propertyBuilder;
   }
}
