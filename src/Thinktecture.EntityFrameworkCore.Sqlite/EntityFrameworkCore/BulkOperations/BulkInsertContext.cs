using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class BulkInsertContext : ISqliteBulkOperationContext
{
   private readonly IReadOnlyList<PropertyWithNavigations> _externalProperties;

   public IEntityDataReaderFactory ReaderFactory { get; }
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqliteConnection Connection { get; }
   public ISqliteBulkInsertOptions Options { get; }

   public bool HasExternalProperties => _externalProperties.Count != 0;
   public SqliteAutoIncrementBehavior AutoIncrementBehavior => Options.AutoIncrementBehavior;

   public BulkInsertContext(
      IEntityDataReaderFactory factory,
      SqliteConnection sqlCon,
      ISqliteBulkInsertOptions options,
      IReadOnlyList<PropertyWithNavigations> properties)
   {
      ReaderFactory = factory;
      Connection = sqlCon;
      var (ownProperties, externalProperties) = properties.SeparateProperties();
      Properties = ownProperties;
      _externalProperties = externalProperties;
      Options = options;
   }

   public SqliteCommandBuilder CreateCommandBuilder()
   {
      return SqliteCommandBuilder.Insert(Properties);
   }

   public IReadOnlyList<ISqliteOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
   {
      if (!HasExternalProperties)
         return Array.Empty<ISqliteOwnedTypeBulkOperationContext>();

      var childCtx = new List<ISqliteOwnedTypeBulkOperationContext>();

      foreach (var (navigation, ownedEntities, properties) in _externalProperties.GroupExternalProperties(entities))
      {
         var ownedTypeCtx = new OwnedTypeBulkInsertContext(ReaderFactory, Connection, Options, properties, navigation.TargetEntityType, ownedEntities);
         childCtx.Add(ownedTypeCtx);
      }

      return childCtx;
   }

   private class OwnedTypeBulkInsertContext : BulkInsertContext, ISqliteOwnedTypeBulkOperationContext
   {
      public IEntityType EntityType { get; }
      public IEnumerable<object> Entities { get; }

      public OwnedTypeBulkInsertContext(
         IEntityDataReaderFactory factory,
         SqliteConnection sqlCon,
         ISqliteBulkInsertOptions options,
         IReadOnlyList<PropertyWithNavigations> properties,
         IEntityType entityType,
         IEnumerable<object> entities)
         : base(factory, sqlCon, options, properties)
      {
         EntityType = entityType;
         Entities = entities;
      }
   }
}
