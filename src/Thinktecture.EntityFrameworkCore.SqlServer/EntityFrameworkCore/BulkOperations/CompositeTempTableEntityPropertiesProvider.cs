using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
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

   public abstract IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType);
   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes);
   public abstract IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes);

   public IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
   {
      return _keyPropertiesProvider.DetermineKeyProperties(entityType);
   }

   private IReadOnlyList<IProperty> GetProperties(
      IEntityType entityType,
      IReadOnlyList<IProperty> propertiesToInsert,
      IReadOnlyList<IProperty> propertiesToUpdate)
   {
      return _keyPropertiesProvider.DetermineKeyProperties(entityType)
                                   .Union(propertiesToInsert)
                                   .Union(propertiesToUpdate)
                                   .ToList();
   }

   private IReadOnlyList<PropertyWithNavigations> GetPropertiesWithNavigations(
      IEntityType entityType,
      IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
      IReadOnlyList<PropertyWithNavigations> propertiesToUpdate)
   {
      return _keyPropertiesProvider.DetermineKeyProperties(entityType).Select(p => new PropertyWithNavigations(p, Array.Empty<Navigation>()))
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

      public override IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType)
      {
         return GetProperties(entityType,
                              _propertiesToInsert.GetPropertiesForTempTable(entityType),
                              _propertiesToUpdate.GetPropertiesForTempTable(entityType));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetPropertiesWithNavigations(entityType,
                                             _propertiesToInsert.GetPropertiesForInsert(entityType, inlinedOwnTypes),
                                             _propertiesToUpdate.GetPropertiesForInsert(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetPropertiesWithNavigations(entityType,
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

      public override IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType)
      {
         return GetProperties(entityType,
                              Array.Empty<IProperty>(),
                              _propertiesToUpdate.GetPropertiesForTempTable(entityType));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetPropertiesWithNavigations(entityType,
                                             Array.Empty<PropertyWithNavigations>(),
                                             _propertiesToUpdate.GetPropertiesForInsert(entityType, inlinedOwnTypes));
      }

      public override IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes)
      {
         return GetPropertiesWithNavigations(entityType,
                                             Array.Empty<PropertyWithNavigations>(),
                                             _propertiesToUpdate.GetPropertiesForUpdate(entityType, inlinedOwnTypes));
      }
   }
}
