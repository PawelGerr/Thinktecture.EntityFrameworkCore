using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
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
      public IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType)
      {
         return GetProperties(entityType,
                              _propertiesToInsert.DeterminePropertiesForTempTable(entityType),
                              _propertiesToUpdate.DeterminePropertiesForTempTable(entityType));
      }

      /// <inheritdoc />
      public IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
      {
         return _keyPropertiesProvider.DetermineKeyProperties(entityType);
      }

      /// <inheritdoc />
      public IReadOnlyList<IProperty> GetPropertiesForInsert(IEntityType entityType)
      {
         return GetProperties(entityType,
                              _propertiesToInsert.DeterminePropertiesForInsert(entityType),
                              _propertiesToUpdate.DeterminePropertiesForInsert(entityType));
      }

      /// <inheritdoc />
      public IReadOnlyList<IProperty> GetPropertiesForUpdate(IEntityType entityType)
      {
         return GetProperties(entityType,
                              _propertiesToInsert.DeterminePropertiesForUpdate(entityType),
                              _propertiesToUpdate.DeterminePropertiesForUpdate(entityType));
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
   }
}
