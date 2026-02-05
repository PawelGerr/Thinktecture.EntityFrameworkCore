using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class BulkInsertContext : ISqliteBulkOperationContext
{
   private readonly DbContext _ctx;
   private readonly IReadOnlyList<PropertyWithNavigations> _externalProperties;
   private readonly IEntityDataReaderFactory _readerFactory;

   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqliteConnection Connection { get; }
   public SqliteBulkInsertOptions Options { get; }

   public bool HasExternalProperties => _externalProperties.Count != 0;

   /// <inheritdoc />
   public IEntityDataReader<T> CreateReader<T>(IEnumerable<T> entities)
   {
      return _readerFactory.Create(_ctx, entities, Properties, HasExternalProperties);
   }

   public SqliteAutoIncrementBehavior AutoIncrementBehavior => Options.AutoIncrementBehavior;

   public BulkInsertContext(
      DbContext ctx,
      IEntityDataReaderFactory factory,
      SqliteConnection sqlCon,
      SqliteBulkInsertOptions options,
      IReadOnlyList<PropertyWithNavigations> properties)
   {
      _ctx = ctx;
      _readerFactory = factory;
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
         var ownedTypeCtx = new OwnedTypeBulkInsertContext(_ctx, _readerFactory, Connection, Options, properties, navigation.TargetEntityType, ownedEntities);
         childCtx.Add(ownedTypeCtx);
      }

      return childCtx;
   }

   private class OwnedTypeBulkInsertContext : BulkInsertContext, ISqliteOwnedTypeBulkOperationContext
   {
      public IEntityType EntityType { get; }
      public IEnumerable<object> Entities { get; }

      public OwnedTypeBulkInsertContext(
         DbContext ctx,
         IEntityDataReaderFactory factory,
         SqliteConnection sqlCon,
         SqliteBulkInsertOptions options,
         IReadOnlyList<PropertyWithNavigations> properties,
         IEntityType entityType,
         IEnumerable<object> entities)
         : base(ctx, factory, sqlCon, options, properties)
      {
         EntityType = entityType;
         Entities = entities;
      }
   }
}
