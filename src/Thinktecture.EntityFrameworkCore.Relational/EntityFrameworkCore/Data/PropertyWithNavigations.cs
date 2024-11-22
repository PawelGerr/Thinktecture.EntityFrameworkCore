using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data;

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
   /// Indication whether this property is persisted in the same table as the parent or in a separate table.
   /// </summary>
   public bool IsInlined { get; }

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
      IsInlined = Navigations.Count == 0 || Navigations.All(n => n.IsInlined());
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
      var hashCode = new HashCode();
      hashCode.Add(Property);
      Navigations.ComputeHashCode(hashCode);

      return hashCode.ToHashCode();
   }

   /// <inheritdoc />
   public override string ToString()
   {
      var path = String.Join(".", Navigations.Select(n => n.Name));

      return String.IsNullOrWhiteSpace(path) ? Property.Name : $"{path}.{Property.Name}";
   }

   /// <summary>
   /// Drops all leading <see cref="Navigations"/> incl. first non-inlined navigation and creates a new <see cref="PropertyWithNavigations"/> from the rest navigations.
   /// </summary>
   /// <returns>Dropped navigations and a new property without the dropped navigations.</returns>
   /// <exception cref="InvalidOperationException">If the current property has no separate navigations.</exception>
   public (IReadOnlyList<INavigation> DroppedNavigations, PropertyWithNavigations Property) DropUntilSeparateNavigation()
   {
      var droppedNavigation = new List<INavigation>();

      for (var i = 0; i < Navigations.Count; i++)
      {
         var navigation = Navigations[i];

         if (navigation.IsInlined())
         {
            droppedNavigation.Add(navigation);
         }
         else
         {
            droppedNavigation.Add(navigation);

            var separateNavigations = new List<INavigation>();

            for (var j = i + 1; j < Navigations.Count; j++)
            {
               separateNavigations.Add(Navigations[j]);
            }

            return (droppedNavigation, new PropertyWithNavigations(Property, separateNavigations));
         }
      }

      throw new InvalidOperationException("This method must not be called for properties without separate navigations.");
   }
}
