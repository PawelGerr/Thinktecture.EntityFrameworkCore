using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal abstract class CompositeTempTableEntityPropertiesProvider : IEntityPropertiesProvider
{
   private readonly IEntityPropertiesProvider? _keyPropertiesProvider;

   protected CompositeTempTableEntityPropertiesProvider(IEntityPropertiesProvider? keyPropertiesProvider)
   {
      _keyPropertiesProvider = keyPropertiesProvider;
   }

   public static IEntityPropertiesProvider CreateForInsertOrUpdate(
      IEntityPropertiesProvider propertiesToInsert,
      IEntityPropertiesProvider propertiesToUpdate,
      IEntityPropertiesProvider? keyPropertiesProvider)
   {
      return new PropertiesProviderForInsertOrUpdate(propertiesToInsert, propertiesToUpdate, keyPropertiesProvider);
   }

   public static IEntityPropertiesProvider CreateForUpdate(
      IEntityPropertiesProvider propertiesToUpdate,
      IEntityPropertiesProvider? keyPropertiesProvider)
   {
      return new PropertiesProviderForUpdate(propertiesToUpdate, keyPropertiesProvider);
   }

   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);

   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);

   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);

   public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      return _keyPropertiesProvider.DetermineKeyProperties(entityType, inlinedOwnTypes);
   }

   private IReadOnlyList<PropertyWithNavigations> GetProperties(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
      IReadOnlyList<PropertyWithNavigations> propertiesToUpdate)
   {
      return _keyPropertiesProvider.DetermineKeyProperties(entityType, inlinedOwnTypes)
                                   .Union(propertiesToInsert)
                                   .Union(propertiesToUpdate)
                                   .ToList();
   }

   private class PropertiesProviderForInsertOrUpdate : CompositeTempTableEntityPropertiesProvider
   {
      private readonly IEntityPropertiesProvider _propertiesToInsert;
      private readonly IEntityPropertiesProvider _propertiesToUpdate;

      public PropertiesProviderForInsertOrUpdate(
         IEntityPropertiesProvider propertiesToInsert,
         IEntityPropertiesProvider propertiesToUpdate,
         IEntityPropertiesProvider? keyPropertiesProvider)
         : base(keyPropertiesProvider)
      {
         _propertiesToInsert = propertiesToInsert;
         _propertiesToUpdate = propertiesToUpdate;
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              _propertiesToInsert.DeterminePropertiesForTempTable(entityType, inlinedOwnTypes),
                              _propertiesToUpdate.DeterminePropertiesForTempTable(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              _propertiesToInsert.DeterminePropertiesForInsert(entityType, inlinedOwnTypes),
                              _propertiesToUpdate.DeterminePropertiesForInsert(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              _propertiesToInsert.DeterminePropertiesForUpdate(entityType, inlinedOwnTypes),
                              _propertiesToUpdate.DeterminePropertiesForUpdate(entityType, inlinedOwnTypes));
      }
   }

   private class PropertiesProviderForUpdate : CompositeTempTableEntityPropertiesProvider
   {
      private readonly IEntityPropertiesProvider _propertiesToUpdate;

      public PropertiesProviderForUpdate(
         IEntityPropertiesProvider propertiesToUpdate,
         IEntityPropertiesProvider? keyPropertiesProvider)
         : base(keyPropertiesProvider)
      {
         _propertiesToUpdate = propertiesToUpdate;
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              Array.Empty<PropertyWithNavigations>(),
                              _propertiesToUpdate.DeterminePropertiesForTempTable(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              Array.Empty<PropertyWithNavigations>(),
                              _propertiesToUpdate.DeterminePropertiesForInsert(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(
         IEntityType entityType,
         bool? inlinedOwnTypes,
         Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              Array.Empty<PropertyWithNavigations>(),
                              _propertiesToUpdate.DeterminePropertiesForUpdate(entityType, inlinedOwnTypes));
      }
   }
}
