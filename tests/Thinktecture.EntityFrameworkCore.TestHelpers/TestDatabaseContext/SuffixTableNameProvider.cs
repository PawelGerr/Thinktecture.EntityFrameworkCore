using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.BulkOperations;

namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// Appends <paramref name="suffix"/> to the table name of the model and keeps the schema of the model.
/// Records every entity type it was asked for, so tests can assert the calls.
/// </summary>
public sealed class SuffixTableNameProvider(string suffix) : IBulkOperationTableNameProvider
{
   public List<IEntityType> RequestedEntityTypes { get; } = [];

   public BulkOperationTableName GetTableName(IEntityType entityType)
   {
      RequestedEntityTypes.Add(entityType);

      return new BulkOperationTableName(entityType.GetTableName() + suffix, entityType.GetSchema());
   }
}
