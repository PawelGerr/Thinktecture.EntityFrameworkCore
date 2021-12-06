using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class CompositeTempTableEntityPropertiesProvider : IEntityPropertiesProvider
{
   private readonly IEntityPropertiesProvider? _propertiesToInsert;
   private readonly IEntityPropertiesProvider? _propertiesToUpdate;
   private readonly IEntityPropertiesProvider? _keyPropertiesProvider;

   public CompositeTempTableEntityPropertiesProvider(
      IEntityPropertiesProvider? propertiesToInsert,
      IEntityPropertiesProvider? propertiesToUpdate,
      IEntityPropertiesProvider? keyPropertiesProvider)
   {
      _propertiesToInsert = propertiesToInsert;
      _propertiesToUpdate = propertiesToUpdate;
      _keyPropertiesProvider = keyPropertiesProvider;
   }

   /// <inheritdoc />
   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      return GetProperties(entityType, inlinedOwnTypes,
                           _propertiesToInsert.DeterminePropertiesForTempTable(entityType, inlinedOwnTypes),
                           _propertiesToUpdate.DeterminePropertiesForTempTable(entityType, inlinedOwnTypes));
   }

   /// <inheritdoc />
   public IReadOnlyList<PropertyWithNavigations> GetKeyProperties(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      return _keyPropertiesProvider.DetermineKeyProperties(entityType, inlinedOwnTypes);
   }

   /// <inheritdoc />
   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      return GetProperties(entityType, inlinedOwnTypes,
                           _propertiesToInsert.DeterminePropertiesForInsert(entityType, inlinedOwnTypes),
                           _propertiesToUpdate.DeterminePropertiesForInsert(entityType, inlinedOwnTypes));
   }

   /// <inheritdoc />
   public IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter)
   {
      return GetProperties(entityType, inlinedOwnTypes,
                           _propertiesToInsert.DeterminePropertiesForUpdate(entityType, inlinedOwnTypes),
                           _propertiesToUpdate.DeterminePropertiesForUpdate(entityType, inlinedOwnTypes));
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
}