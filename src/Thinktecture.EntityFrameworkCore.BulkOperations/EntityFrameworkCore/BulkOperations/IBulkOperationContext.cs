using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk operation context.
/// </summary>
public interface IBulkOperationContext
{
   /// <summary>
   /// Properties participating in the bulk operation.
   /// </summary>
   IReadOnlyList<PropertyWithNavigations> Properties { get; }

   /// <summary>
   /// Indication whether there are properties that belongs to a different table.
   /// </summary>
   bool HasExternalProperties { get; }

   /// <summary>
   /// Creates a new <see cref="IEntityDataReader{T}"/>.
   /// </summary>
   /// <param name="entities"></param>
   /// <typeparam name="T"></typeparam>
   /// <returns></returns>
   IEntityDataReader<T> CreateReader<T>(IEnumerable<T> entities);
}
