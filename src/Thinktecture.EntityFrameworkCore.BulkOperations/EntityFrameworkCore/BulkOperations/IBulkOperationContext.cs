using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk operation context.
/// </summary>
public interface IBulkOperationContext
{
   /// <summary>
   /// Factory for <see cref="IEntityDataReader{T}"/>.
   /// </summary>
   IEntityDataReaderFactory ReaderFactory { get; }

   /// <summary>
   /// Properties participating in the bulk operation.
   /// </summary>
   IReadOnlyList<PropertyWithNavigations> Properties { get; }

   /// <summary>
   /// Indication whether there are properties that belongs to a different table.
   /// </summary>
   bool HasExternalProperties { get; }

   /// <summary>
   /// Gets bulk operation contexts for "external" properties, i.e. belonging to other tables.
   /// </summary>
   /// <param name="entities"></param>
   /// <returns></returns>
   IReadOnlyList<IOwnedTypeBulkOperationContext> GetContextsForExternalOwnedTypes(IReadOnlyList<object> entities);
}
