using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Contains the property of an entity and the navigations to reach this property.
   /// </summary>
   public readonly struct PropertyWithNavigations : IEquatable<PropertyWithNavigations>
   {
      /// <summary>
      /// A property of an entity.
      /// </summary>
      public IProperty Property { get; }

      /// <summary>
      /// Navigations to reach the <see cref="Property"/>.
      /// </summary>
      public IReadOnlyList<INavigation> Navigations { get; }

      /// <summary>
      /// Indication whether this property is part of a separate owned typed.
      /// </summary>
      public bool BelongsToSeparateOwnedType { get; }

      /// <summary>
      /// Initializes a new instance of <see cref="PropertyWithNavigations"/>.
      /// </summary>
      /// <param name="property">A property of an entity.</param>
      /// <param name="navigations">Navigations to reach the <paramref name="property"/>.</param>
      public PropertyWithNavigations(
         IProperty property,
         IReadOnlyList<INavigation> navigations)
      {
         Property = property;
         Navigations = navigations;
         BelongsToSeparateOwnedType = Navigations.Count != 0 && !Navigations[0].IsOwnedTypeInline();
      }

      /// <inheritdoc />
      public override bool Equals(object? obj)
      {
         return obj is PropertyWithNavigations other && Equals(other);
      }

      /// <inheritdoc />
      public bool Equals(PropertyWithNavigations other)
      {
         return Property.Equals(other.Property) && Equals(other.Navigations);
      }

      private bool Equals(IReadOnlyList<INavigation> other)
      {
         if (Navigations.Count != other.Count)
            return false;

         for (var i = 0; i < Navigations.Count; i++)
         {
            var navigation = Navigations[i];
            var otherNavi = other[i];

            if (!navigation.Equals(otherNavi))
               return false;
         }

         return true;
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return HashCode.Combine(Property, Navigations);
      }

      /// <inheritdoc />
      public override string ToString()
      {
         var path = String.Join(".", Navigations.Select(n => n.Name));

         return String.IsNullOrWhiteSpace(path) ? Property.Name : $"{path}.{Property.Name}";
      }
   }
}
