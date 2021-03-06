using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations
{
   internal class BulkInsertOrUpdateContext : ISqliteBulkOperationContext
   {
      private readonly IReadOnlyList<PropertyWithNavigations> _keyProperties;
      private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToInsert;
      private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToUpdate;
      private readonly IReadOnlyList<PropertyWithNavigations> _externalPropertiesToInsert;
      private readonly IReadOnlyList<PropertyWithNavigations> _externalPropertiesToUpdate;

      public IEntityDataReaderFactory ReaderFactory { get; }
      public IReadOnlyList<PropertyWithNavigations> Properties { get; }
      public bool HasExternalProperties => _externalPropertiesToInsert.Count != 0 || _externalPropertiesToUpdate.Count != 0;

      public SqliteAutoIncrementBehavior AutoIncrementBehavior => SqliteAutoIncrementBehavior.KeepValueAsIs;
      public SqliteConnection Connection { get; }

      public BulkInsertOrUpdateContext(
         IEntityDataReaderFactory factory,
         SqliteConnection connection,
         IReadOnlyList<PropertyWithNavigations> keyProperties,
         IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
         IReadOnlyList<PropertyWithNavigations> propertiesForUpdate)
      {
         ReaderFactory = factory;
         Connection = connection;
         _keyProperties = keyProperties;

         var (ownPropertiesToInsert, externalPropertiesToInsert) = propertiesToInsert.SeparateProperties();
         _propertiesToInsert = ownPropertiesToInsert;
         _externalPropertiesToInsert = externalPropertiesToInsert;

         var (ownPropertiesToUpdate, externalPropertiesToUpdate) = propertiesForUpdate.SeparateProperties();
         _propertiesToUpdate = ownPropertiesToUpdate;
         _externalPropertiesToUpdate = externalPropertiesToUpdate;

         Properties = ownPropertiesToInsert.Union(ownPropertiesToUpdate).Union(keyProperties).ToList();
      }

      /// <inheritdoc />
      public SqliteCommandBuilder CreateCommandBuilder()
      {
         return SqliteCommandBuilder.InsertOrUpdate(_propertiesToInsert, _propertiesToUpdate, _keyProperties);
      }

      public IReadOnlyList<ISqliteOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
      {
         if (!HasExternalProperties)
            return Array.Empty<ISqliteOwnedTypeBulkOperationContext>();

         var childCtx = new List<ISqliteOwnedTypeBulkOperationContext>();

         var propertiesToUpdateData = _externalPropertiesToUpdate.GroupExternalProperties(entities).ToList();

         foreach (var (navigation, ownedEntities, propertiesToInsert) in _externalPropertiesToInsert.GroupExternalProperties(entities))
         {
            var propertiesToUpdateTuple = propertiesToUpdateData.FirstOrDefault(d => d.Item1 == navigation);
            var propertiesToUpdate = propertiesToUpdateTuple.Item3;

            if (propertiesToUpdate is null || propertiesToUpdate.Count == 0)
               throw new Exception($"The owned type property '{navigation.DeclaringEntityType.Name}.{navigation.Name}' is selected for bulk-insert-or-update but there are no properties for performing the update.");

            propertiesToUpdateData.Remove(propertiesToUpdateTuple);

            var ownedTypeCtx = new OwnedTypeBulkInsertOrUpdateContext(ReaderFactory, Connection, propertiesToInsert, propertiesToUpdate, navigation.TargetEntityType, ownedEntities);
            childCtx.Add(ownedTypeCtx);
         }

         if (propertiesToUpdateData.Count != 0)
         {
            var updateWithoutInsert = propertiesToUpdateData[0].Item1;
            throw new Exception($"{propertiesToUpdateData.Count} owned type property/properties including '{updateWithoutInsert.DeclaringEntityType.Name}.{updateWithoutInsert.Name}' is selected for bulk-insert-or-update but there are no properties for performing the insert.");
         }

         return childCtx;
      }

      private class OwnedTypeBulkInsertOrUpdateContext : BulkInsertOrUpdateContext, ISqliteOwnedTypeBulkOperationContext
      {
         public IEntityType EntityType { get; }
         public IEnumerable<object> Entities { get; }

         public OwnedTypeBulkInsertOrUpdateContext(
            IEntityDataReaderFactory factory,
            SqliteConnection sqlCon,
            IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
            IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
            IEntityType entityType,
            IEnumerable<object> entities)
            : base(factory, sqlCon, GetKeyProperties(entityType), propertiesToInsert, propertiesToUpdate)
         {
            EntityType = entityType;
            Entities = entities;
         }

         private static IReadOnlyList<PropertyWithNavigations> GetKeyProperties(IEntityType entityType)
         {
            var properties = entityType.FindPrimaryKey()?.Properties;

            if (properties is null || properties.Count == 0)
               throw new Exception($"The entity type '{entityType.Name}' needs a primary key to be able to perform bulk-insert-or-update.");

            return properties.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
         }
      }
   }
}
