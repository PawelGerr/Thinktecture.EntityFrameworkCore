using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class BulkInsertOrUpdateContext : ISqliteBulkOperationContext
{
   private readonly IReadOnlyList<IProperty> _keyProperties;
   private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToInsert;
   private readonly IReadOnlyList<PropertyWithNavigations> _propertiesToUpdate;
   private readonly IReadOnlyList<PropertyWithNavigations> _externalPropertiesToInsert;
   private readonly IReadOnlyList<PropertyWithNavigations> _externalPropertiesToUpdate;
   private readonly DbContext _ctx;
   private readonly IEntityDataReaderFactory _readerFactory;

   public IReadOnlyList<PropertyWithNavigations> Properties { get; }
   public bool HasExternalProperties => _externalPropertiesToInsert.Count != 0 || _externalPropertiesToUpdate.Count != 0;

   /// <inheritdoc />
   public IEntityDataReader<T> CreateReader<T>(IEnumerable<T> entities)
   {
      return _readerFactory.Create(_ctx, entities, Properties, HasExternalProperties);
   }

   public SqliteAutoIncrementBehavior AutoIncrementBehavior { get; }
   public SqliteConnection Connection { get; }

   public BulkInsertOrUpdateContext(
      DbContext ctx,
      IEntityDataReaderFactory factory,
      SqliteConnection connection,
      IReadOnlyList<IProperty> keyProperties,
      IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
      IReadOnlyList<PropertyWithNavigations> propertiesForUpdate,
      SqliteAutoIncrementBehavior autoIncrementBehavior)
   {
      _ctx = ctx;
      _readerFactory = factory;
      Connection = connection;
      _keyProperties = keyProperties;
      AutoIncrementBehavior = autoIncrementBehavior;

      var (ownPropertiesToInsert, externalPropertiesToInsert) = propertiesToInsert.SeparateProperties();
      _propertiesToInsert = ownPropertiesToInsert;
      _externalPropertiesToInsert = externalPropertiesToInsert;

      var (ownPropertiesToUpdate, externalPropertiesToUpdate) = propertiesForUpdate.SeparateProperties();
      _propertiesToUpdate = ownPropertiesToUpdate;
      _externalPropertiesToUpdate = externalPropertiesToUpdate;

      // Owned types in separate tables need properties for the insert and for the update.
      // The check runs here, before the first write, so that a wrong configuration does not lead to a partial write.
      EnsureInsertAndUpdatePropertiesForSeparateOwnedTypes(externalPropertiesToInsert, externalPropertiesToUpdate);

      Properties = ownPropertiesToInsert.Union(ownPropertiesToUpdate).Union(keyProperties.Select(p => new PropertyWithNavigations(p, Array.Empty<Navigation>()))).ToList();
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

      // The constructor ensures that every owned type has properties for the insert and for the update.
      var propertiesToUpdateData = _externalPropertiesToUpdate.GroupExternalProperties(entities).ToList();

      foreach (var (navigation, ownedEntities, propertiesToInsert) in _externalPropertiesToInsert.GroupExternalProperties(entities))
      {
         var propertiesToUpdate = propertiesToUpdateData.First(d => d.Item1 == navigation).Item3;

         var ownedTypeCtx = new OwnedTypeBulkInsertOrUpdateContext(_ctx, _readerFactory, Connection, propertiesToInsert, propertiesToUpdate, navigation.TargetEntityType, ownedEntities, AutoIncrementBehavior);
         childCtx.Add(ownedTypeCtx);
      }

      return childCtx;
   }

   private static void EnsureInsertAndUpdatePropertiesForSeparateOwnedTypes(
      IReadOnlyList<PropertyWithNavigations> externalPropertiesToInsert,
      IReadOnlyList<PropertyWithNavigations> externalPropertiesToUpdate)
   {
      var navigationsToInsert = externalPropertiesToInsert.Select(GetSeparateNavigation).Distinct().ToList();
      var navigationsToUpdate = externalPropertiesToUpdate.Select(GetSeparateNavigation).Distinct().ToList();

      var insertWithoutUpdate = navigationsToInsert.FirstOrDefault(n => !navigationsToUpdate.Contains(n));

      if (insertWithoutUpdate is not null)
         throw new NotSupportedException($"The owned type property '{insertWithoutUpdate.DeclaringEntityType.Name}.{insertWithoutUpdate.Name}' is selected for bulk-insert-or-update but there are no properties for performing the update.");

      var updatesWithoutInsert = navigationsToUpdate.Where(n => !navigationsToInsert.Contains(n)).ToList();

      if (updatesWithoutInsert.Count != 0)
         throw new NotSupportedException($"{updatesWithoutInsert.Count} owned type property/properties including '{updatesWithoutInsert[0].DeclaringEntityType.Name}.{updatesWithoutInsert[0].Name}' is selected for bulk-insert-or-update but there are no properties for performing the insert.");
   }

   private static INavigation GetSeparateNavigation(PropertyWithNavigations property)
   {
      return property.DropUntilSeparateNavigation().DroppedNavigations[^1];
   }

   private class OwnedTypeBulkInsertOrUpdateContext : BulkInsertOrUpdateContext, ISqliteOwnedTypeBulkOperationContext
   {
      public IEntityType EntityType { get; }
      public IEnumerable<object> Entities { get; }

      public OwnedTypeBulkInsertOrUpdateContext(
         DbContext ctx,
         IEntityDataReaderFactory factory,
         SqliteConnection sqlCon,
         IReadOnlyList<PropertyWithNavigations> propertiesToInsert,
         IReadOnlyList<PropertyWithNavigations> propertiesToUpdate,
         IEntityType entityType,
         IEnumerable<object> entities,
         SqliteAutoIncrementBehavior autoIncrementBehavior)
         : base(ctx, factory, sqlCon, GetKeyProperties(entityType), propertiesToInsert, propertiesToUpdate, autoIncrementBehavior)
      {
         EntityType = entityType;
         Entities = entities;
      }

      private static IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType)
      {
         var properties = entityType.FindPrimaryKey()?.Properties;

         if (properties is null || properties.Count == 0)
            throw new Exception($"The entity type '{entityType.Name}' needs a primary key to be able to perform bulk-insert-or-update.");

         return properties;
      }
   }
}
