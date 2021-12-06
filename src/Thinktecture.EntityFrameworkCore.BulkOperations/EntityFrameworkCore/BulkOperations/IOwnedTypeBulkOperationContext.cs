using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Bulk operation context for an owned type.
/// </summary>
public interface IOwnedTypeBulkOperationContext : IBulkOperationContext
{
   /// <summary>
   /// Type of the owned type.
   /// </summary>
   IEntityType EntityType { get; }

   /// <summary>
   /// A collection of owned types.
   /// </summary>
   IEnumerable<object> Entities { get; }
}
