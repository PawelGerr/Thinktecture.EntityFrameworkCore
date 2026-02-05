using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class BulkUpdateContext : ISqliteBulkOperationContext
{
   private readonly IReadOnlyList<IProperty> _keyProperties;
   private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToUpdate;
   private readonly IReadOnlyList<PropertyWithNavigations> _externalProperties;
   private readonly DbContext _ctx;
   private readonly IEntityDataReaderFactory _readerFactory;

   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqliteConnection Connection { get; }

   public bool HasExternalProperties => _externalProperties.Count != 0;

   /// <inheritdoc />
   public IEntityDataReader<T> CreateReader<T>(IEnumerable<T> entities)
   {
      return _readerFactory.Create(_ctx, entities, Properties, HasExternalProperties);
   }

   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; }

   public BulkUpdateContext(
      DbContext ctx,
      IEntityDataReaderFactory factory,
      SqliteConnection connection,
      IReadOnlyList<IProperty> keyProperties,
      IReadOnlyList<PropertyWithNavigations> propertiesForUpdate,
      SqliteAutoIncrementBehavior autoIncrementBehavior)
   {
      _ctx = ctx;
      _readerFactory = factory;
      Connection = connection;
      _keyProperties = keyProperties;
      var (ownProperties, externalProperties) = propertiesForUpdate.SeparateProperties();
      _propertiesToUpdate = ownProperties;
      _externalProperties = externalProperties;
      Properties = ownProperties.Union(keyProperties.Select(p => new PropertyWithNavigations(p, Array.Empty<Navigation>()))).ToList();
      AutoIncrementBehavior = autoIncrementBehavior;
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
         var ownedTypeCtx = new OwnedTypeBulkUpdateContext(_ctx, _readerFactory, Connection, properties, navigation.TargetEntityType, ownedEntities, AutoIncrementBehavior);
         childCtx.Add(ownedTypeCtx);
      }

      return childCtx;
   }

   private class OwnedTypeBulkUpdateContext : BulkUpdateContext, ISqliteOwnedTypeBulkOperationContext
   {
      public IEntityType EntityType { get; }
      public IEnumerable<object> Entities { get; }

      public OwnedTypeBulkUpdateContext(
         DbContext ctx,
         IEntityDataReaderFactory factory,
         SqliteConnection sqlCon,
         IReadOnlyList<PropertyWithNavigations> properties,
         IEntityType entityType,
         IEnumerable<object> entities,
         SqliteAutoIncrementBehavior autoIncrementBehavior)
         : base(ctx, factory, sqlCon, GetKeyProperties(entityType), properties, autoIncrementBehavior)
      {
         EntityType = entityType;
         Entities = entities;
      }

      private static IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
      {
         var properties = entityType.FindPrimaryKey()?.Properties;

         if (properties is null || properties.Count == 0)
            throw new Exception($"The entity type '{entityType.Name}' needs a primary key to be able to perform bulk-update.");

         return properties;
      }
   }
}
