using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Thinktecture.EntityFrameworkCore.Conventions;

/// <summary>
/// A convention type.
/// </summary>
public abstract class ConventionType
{
   /// <summary>
   /// Conventions to run to setup the initial model.
   /// </summary>
   public static readonly ConventionType ModelInitializedConventions = Create("ModelInitializedConventions", set => set.ModelInitializedConventions);

   /// <summary>
   /// Conventions to run when model building is completed.
   /// </summary>
   public static readonly ConventionType ModelFinalizingConventions = Create("ModelFinalizingConventions", set => set.ModelFinalizingConventions);

   /// <summary>
   /// Conventions to run when model validation is completed.
   /// </summary>
   public static readonly ConventionType ModelFinalizedConventions = Create("ModelFinalizedConventions", set => set.ModelFinalizedConventions);

   /// <summary>
   /// Conventions to run when an annotation is set or removed on a model.
   /// </summary>
   public static readonly ConventionType ModelAnnotationChangedConventions = Create("ModelAnnotationChangedConventions", set => set.ModelAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when an entity type is added to the model.
   /// </summary>
   public static readonly ConventionType EntityTypeAddedConventions = Create("EntityTypeAddedConventions", set => set.EntityTypeAddedConventions);

   /// <summary>
   /// Conventions to run when an entity type is ignored.
   /// </summary>
   public static readonly ConventionType EntityTypeIgnoredConventions = Create("EntityTypeIgnoredConventions", set => set.EntityTypeIgnoredConventions);

   /// <summary>
   /// Conventions to run when an entity type is removed.
   /// </summary>
   public static readonly ConventionType EntityTypeRemovedConventions = Create("EntityTypeRemovedConventions", set => set.EntityTypeRemovedConventions);

   /// <summary>
   /// Conventions to run when a property is ignored.
   /// </summary>
   public static readonly ConventionType EntityTypeMemberIgnoredConventions = Create("EntityTypeMemberIgnoredConventions", set => set.EntityTypeMemberIgnoredConventions);

   /// <summary>
   /// Conventions to run when the base entity type is changed.
   /// </summary>
   public static readonly ConventionType EntityTypeBaseTypeChangedConventions = Create("EntityTypeBaseTypeChangedConventions", set => set.EntityTypeBaseTypeChangedConventions);

   /// <summary>
   /// Conventions to run when a primary key is changed.
   /// </summary>
   public static readonly ConventionType EntityTypePrimaryKeyChangedConventions = Create("EntityTypePrimaryKeyChangedConventions", set => set.EntityTypePrimaryKeyChangedConventions);

   /// <summary>
   /// Conventions to run when an annotation is set or removed on an entity type.
   /// </summary>
   public static readonly ConventionType EntityTypeAnnotationChangedConventions = Create("EntityTypeAnnotationChangedConventions", set => set.EntityTypeAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when a foreign key is added.
   /// </summary>
   public static readonly ConventionType ForeignKeyAddedConventions = Create("ForeignKeyAddedConventions", set => set.ForeignKeyAddedConventions);

   /// <summary>
   /// Conventions to run when a foreign key is removed.
   /// </summary>
   public static readonly ConventionType ForeignKeyRemovedConventions = Create("ForeignKeyRemovedConventions", set => set.ForeignKeyRemovedConventions);

   /// <summary>
   /// Conventions to run when the principal end of a relationship is configured.
   /// </summary>
   public static readonly ConventionType ForeignKeyPrincipalEndChangedConventions = Create("ForeignKeyPrincipalEndChangedConventions", set => set.ForeignKeyPrincipalEndChangedConventions);

   /// <summary>
   /// Conventions to run when the properties or the principal key of a foreign key are changed.
   /// </summary>
   public static readonly ConventionType ForeignKeyPropertiesChangedConventions = Create("ForeignKeyPropertiesChangedConventions", set => set.ForeignKeyPropertiesChangedConventions);

   /// <summary>
   /// Conventions to run when the uniqueness of a foreign key is changed.
   /// </summary>
   public static readonly ConventionType ForeignKeyUniquenessChangedConventions = Create("ForeignKeyUniquenessChangedConventions", set => set.ForeignKeyUniquenessChangedConventions);

   /// <summary>
   /// Conventions to run when the requiredness of a foreign key is changed.
   /// </summary>
   public static readonly ConventionType ForeignKeyRequirednessChangedConventions = Create("ForeignKeyRequirednessChangedConventions", set => set.ForeignKeyRequirednessChangedConventions);

   /// <summary>
   /// Conventions to run when the requiredness of a foreign key is changed.
   /// </summary>
   public static readonly ConventionType ForeignKeyDependentRequirednessChangedConventions = Create("ForeignKeyDependentRequirednessChangedConventions", set => set.ForeignKeyDependentRequirednessChangedConventions);

   /// <summary>
   /// Conventions to run when the ownership of a foreign key is changed.
   /// </summary>
   public static readonly ConventionType ForeignKeyOwnershipChangedConventions = Create("ForeignKeyOwnershipChangedConventions", set => set.ForeignKeyOwnershipChangedConventions);

   /// <summary>
   /// Conventions to run when an annotation is changed on a foreign key.
   /// </summary>
   public static readonly ConventionType ForeignKeyAnnotationChangedConventions = Create("ForeignKeyAnnotationChangedConventions", set => set.ForeignKeyAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when a navigation is set to <see langword="null" /> on a foreign key.
   /// </summary>
   public static readonly ConventionType ForeignKeyNullNavigationSetConventions = Create("ForeignKeyNullNavigationSetConventions", set => set.ForeignKeyNullNavigationSetConventions);

   /// <summary>
   /// Conventions to run when a navigation property is added.
   /// </summary>
   public static readonly ConventionType NavigationAddedConventions = Create("NavigationAddedConventions", set => set.NavigationAddedConventions);

   /// <summary>
   /// Conventions to run when an annotation is changed on a navigation property.
   /// </summary>
   public static readonly ConventionType NavigationAnnotationChangedConventions = Create("NavigationAnnotationChangedConventions", set => set.NavigationAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when a navigation property is removed.
   /// </summary>
   public static readonly ConventionType NavigationRemovedConventions = Create("NavigationRemovedConventions", set => set.NavigationRemovedConventions);

   /// <summary>
   /// Conventions to run when a skip navigation property is added.
   /// </summary>
   public static readonly ConventionType SkipNavigationAddedConventions = Create("SkipNavigationAddedConventions", set => set.SkipNavigationAddedConventions);

   /// <summary>
   /// Conventions to run when an annotation is changed on a skip navigation property.
   /// </summary>
   public static readonly ConventionType SkipNavigationAnnotationChangedConventions = Create("SkipNavigationAnnotationChangedConventions", set => set.SkipNavigationAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when a skip navigation foreign key is changed.
   /// </summary>
   public static readonly ConventionType SkipNavigationForeignKeyChangedConventions = Create("SkipNavigationForeignKeyChangedConventions", set => set.SkipNavigationForeignKeyChangedConventions);

   /// <summary>
   /// Conventions to run when a skip navigation inverse is changed.
   /// </summary>
   public static readonly ConventionType SkipNavigationInverseChangedConventions = Create("SkipNavigationInverseChangedConventions", set => set.SkipNavigationInverseChangedConventions);

   /// <summary>
   /// Conventions to run when a skip navigation property is removed.
   /// </summary>
   public static readonly ConventionType SkipNavigationRemovedConventions = Create("SkipNavigationRemovedConventions", set => set.SkipNavigationRemovedConventions);

   /// <summary>
   /// Conventions to run when a key is added.
   /// </summary>
   public static readonly ConventionType KeyAddedConventions = Create("KeyAddedConventions", set => set.KeyAddedConventions);

   /// <summary>
   /// Conventions to run when a key is removed.
   /// </summary>
   public static readonly ConventionType KeyRemovedConventions = Create("KeyRemovedConventions", set => set.KeyRemovedConventions);

   /// <summary>
   /// Conventions to run when an annotation is changed on a key.
   /// </summary>
   public static readonly ConventionType KeyAnnotationChangedConventions = Create("KeyAnnotationChangedConventions", set => set.KeyAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when an index is added.
   /// </summary>
   public static readonly ConventionType IndexAddedConventions = Create("IndexAddedConventions", set => set.IndexAddedConventions);

   /// <summary>
   /// Conventions to run when an index is removed.
   /// </summary>
   public static readonly ConventionType IndexRemovedConventions = Create("IndexRemovedConventions", set => set.IndexRemovedConventions);

   /// <summary>
   /// Conventions to run when the uniqueness of an index is changed.
   /// </summary>
   public static readonly ConventionType IndexUniquenessChangedConventions = Create("IndexUniquenessChangedConventions", set => set.IndexUniquenessChangedConventions);

   /// <summary>
   /// Conventions to run when an annotation is changed on an index.
   /// </summary>
   public static readonly ConventionType IndexAnnotationChangedConventions = Create("IndexAnnotationChangedConventions", set => set.IndexAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when a property is added.
   /// </summary>
   public static readonly ConventionType PropertyAddedConventions = Create("PropertyAddedConventions", set => set.PropertyAddedConventions);

   /// <summary>
   /// Conventions to run when the nullability of a property is changed.
   /// </summary>
   public static readonly ConventionType PropertyNullabilityChangedConventions = Create("PropertyNullabilityChangedConventions", set => set.PropertyNullabilityChangedConventions);

   /// <summary>
   /// Conventions to run when the field of a property is changed.
   /// </summary>
   public static readonly ConventionType PropertyFieldChangedConventions = Create("PropertyFieldChangedConventions", set => set.PropertyFieldChangedConventions);

   /// <summary>
   /// Conventions to run when an annotation is changed on a property.
   /// </summary>
   public static readonly ConventionType PropertyAnnotationChangedConventions = Create("PropertyAnnotationChangedConventions", set => set.PropertyAnnotationChangedConventions);

   /// <summary>
   /// Conventions to run when a property is removed.
   /// </summary>
   public static readonly ConventionType PropertyRemovedConventions = Create("PropertyRemovedConventions", set => set.PropertyRemovedConventions);

   /// <summary>
   /// The name collection the conventions of this type reside in <see cref="ConventionSet"/>.
   /// </summary>
   public string CollectionName { get; }

   /// <summary>
   /// The type of the interface.
   /// </summary>
   public Type InterfaceType { get; }

   private ConventionType(string collectionName, Type interfaceType)
   {
      CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
      InterfaceType = interfaceType ?? throw new ArgumentNullException(nameof(interfaceType));
   }

   /// <inheritdoc />
   public override string ToString()
   {
      return InterfaceType.ShortDisplayName();
   }

   /// <summary>
   /// Removes the <paramref name="conventionToRemove"/> from the provided <paramref name="conventionSet"/>.
   /// </summary>
   /// <param name="conventionSet">Convention set to remove the <paramref name="conventionToRemove"/> from.</param>
   /// <param name="conventionToRemove">Implementation type of the convention to remove.</param>
   /// <returns><c>true</c> if the <paramref name="conventionToRemove"/> is found and removed from <paramref name="conventionSet"/>; otherwise <c>false</c>.</returns>
   public abstract bool RemoveConvention(ConventionSet conventionSet, Type conventionToRemove);

   private static GenericConventionType<TConvention> Create<TConvention>(string name, Func<ConventionSet, IList<TConvention>> conventionSelector)
   {
      return new GenericConventionType<TConvention>(name, conventionSelector);
   }

   private class GenericConventionType<TConvention> : ConventionType
   {
      private readonly Func<ConventionSet, IList<TConvention>> _conventionSelector;

      public GenericConventionType(string collectionName, Func<ConventionSet, IList<TConvention>> conventionSelector)
         : base(collectionName, typeof(TConvention))
      {
         _conventionSelector = conventionSelector;
      }

      public override bool RemoveConvention(ConventionSet conventionSet, Type conventionToRemove)
      {
         return ConventionSet.Remove(_conventionSelector(conventionSet), conventionToRemove);
      }
   }
}
