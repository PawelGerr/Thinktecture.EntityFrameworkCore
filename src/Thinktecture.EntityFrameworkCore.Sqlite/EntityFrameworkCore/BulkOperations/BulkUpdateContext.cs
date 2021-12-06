using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class BulkUpdateContext : ISqliteBulkOperationContext
{
   private readonly IReadOnlyList<PropertyWithNavigations> _keyProperties;
   private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToUpdate;
   private readonly IReadOnlyList<PropertyWithNavigations> _externalProperties;

   public IEntityDataReaderFactory ReaderFactory { get; }
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqliteConnection Connection { get; }

   public bool HasExternalProperties => _externalProperties.Count != 0;
   public SqliteAutoIncrementBehavior AutoIncrementBehavior => SqliteAutoIncrementBehavior.KeepValueAsIs;

   public BulkUpdateContext(
      IEntityDataReaderFactory factory,
      SqliteConnection connection,
      IReadOnlyList<PropertyWithNavigations> keyProperties,
      IReadOnlyList<PropertyWithNavigations> propertiesForUpdate)
   {
      ReaderFactory = factory;
      Connection = connection;
      _keyProperties = keyProperties;
      var (ownProperties, externalProperties) = propertiesForUpdate.SeparateProperties();
      _propertiesToUpdate = ownProperties;
      _externalProperties = externalProperties;
      Properties = ownProperties.Union(keyProperties).ToList();
   }

   /// <inheritdoc />
   public SqliteCommandBuilder CreateCommandBuilder()
   {
      return SqliteCommandBuilder.Update(_propertiesToUpdate, _keyProperties);
   }

   /// <inheritdoc />
   public IReadOnlyList<ISqliteOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
   {
      if (!HasExternalProperties)
         return Array.Empty<ISqliteOwnedTypeBulkOperationContext>();

      var childCtx = new List<ISqliteOwnedTypeBulkOperationContext>();

      foreach (var (navigation, ownedEntities, properties) in _externalProperties.GroupExternalProperties(entities))
      {
         var ownedTypeCtx = new OwnedTypeBulkUpdateContext(ReaderFactory, Connection, properties, navigation.TargetEntityType, ownedEntities);
         childCtx.Add(ownedTypeCtx);
      }

      return childCtx;
   }

   private class OwnedTypeBulkUpdateContext : BulkUpdateContext, ISqliteOwnedTypeBulkOperationContext
   {
      public IEntityType EntityType { get; }
      public IEnumerable<object> Entities { get; }

      public OwnedTypeBulkUpdateContext(
         IEntityDataReaderFactory factory,
         SqliteConnection sqlCon,
         IReadOnlyList<PropertyWithNavigations> properties,
         IEntityType entityType,
         IEnumerable<object> entities)
         : base(factory, sqlCon, GetKeyProperties(entityType), properties)
      {
         EntityType = entityType;
         Entities = entities;
      }

      private static IReadOnlyList<PropertyWithNavigations> GetKeyProperties(IEntityType entityType)
      {
         var properties = entityType.FindPrimaryKey()?.Properties;

         if (properties is null || properties.Count == 0)
            throw new Exception($"The entity type '{entityType.Name}' needs a primary key to be able to perform bulk-update.");

         return properties.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      }
   }
}
