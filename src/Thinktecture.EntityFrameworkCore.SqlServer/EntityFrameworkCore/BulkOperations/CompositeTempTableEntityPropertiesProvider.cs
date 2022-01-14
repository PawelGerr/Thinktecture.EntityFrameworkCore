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

   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(IEntityType entityType, bool? inlinedOwnTypes);
   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes);
   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes);

   public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(IEntityType entityType, bool? inlinedOwnTypes)
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

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              _propertiesToInsert.GetPropertiesForTempTable(entityType, inlinedOwnTypes),
                              _propertiesToUpdate.GetPropertiesForTempTable(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              _propertiesToInsert.GetPropertiesForInsert(entityType, inlinedOwnTypes),
                              _propertiesToUpdate.GetPropertiesForInsert(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              _propertiesToInsert.GetPropertiesForUpdate(entityType, inlinedOwnTypes),
                              _propertiesToUpdate.GetPropertiesForUpdate(entityType, inlinedOwnTypes));
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

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              Array.Empty<PropertyWithNavigations>(),
                              _propertiesToUpdate.GetPropertiesForTempTable(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              Array.Empty<PropertyWithNavigations>(),
                              _propertiesToUpdate.GetPropertiesForInsert(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetProperties(entityType, inlinedOwnTypes,
                              Array.Empty<PropertyWithNavigations>(),
                              _propertiesToUpdate.GetPropertiesForUpdate(entityType, inlinedOwnTypes));
      }
   }
}
