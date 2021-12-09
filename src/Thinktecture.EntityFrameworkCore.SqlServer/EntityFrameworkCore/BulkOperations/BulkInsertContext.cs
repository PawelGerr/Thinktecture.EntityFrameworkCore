using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

internal class BulkInsertContext : ISqlServerBulkOperationContext
{
   private readonly IReadOnlyList<PropertyWithNavigations> _externalProperties;

   public IEntityDataReaderFactory ReaderFactory { get; }
   public SqlConnection Connection { get; }
   public SqlTransaction? Transaction { get; }
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public SqlServerBulkInsertOptions Options { get; }
   public bool HasExternalProperties => _externalProperties.Count != 0;

   public BulkInsertContext(
      IEntityDataReaderFactory factory,
      SqlConnection connection,
      SqlTransaction? transaction,
      SqlServerBulkInsertOptions options,
      IReadOnlyList<PropertyWithNavigations> properties)
   {
      ReaderFactory = factory;
      Connection = connection;
      Transaction = transaction;
      var (ownProperties, externalProperties) = properties.SeparateProperties();
      Properties = ownProperties;
      _externalProperties = externalProperties;
      Options = options;
   }

   public IReadOnlyList<ISqlServerOwnedTypeBulkOperationContext> GetChildren(IReadOnlyList<object> entities)
   {
      if (!HasExternalProperties)
         return Array.Empty<ISqlServerOwnedTypeBulkOperationContext>();

      var childCtx = new List<ISqlServerOwnedTypeBulkOperationContext>();

      foreach (var (navigation, ownedEntities, properties) in _externalProperties.GroupExternalProperties(entities))
      {
         var ownedTypeCtx = new OwnedTypeBulkInsertContext(ReaderFactory, Connection, Transaction, Options, properties, navigation.TargetEntityType, ownedEntities);
         childCtx.Add(ownedTypeCtx);
      }

      return childCtx;
   }

   private class OwnedTypeBulkInsertContext : BulkInsertContext, ISqlServerOwnedTypeBulkOperationContext
   {
      public IEntityType EntityType { get; }
      public IEnumerable<object> Entities { get; }

      public OwnedTypeBulkInsertContext(
         IEntityDataReaderFactory factory,
         SqlConnection connection,
         SqlTransaction? transaction,
         SqlServerBulkInsertOptions options,
         IReadOnlyList<PropertyWithNavigations> properties,
         IEntityType entityType,
         IEnumerable<object> entities)
         : base(factory, connection, transaction, options, properties)
      {
         EntityType = entityType;
         Entities = entities;
      }
   }
}
