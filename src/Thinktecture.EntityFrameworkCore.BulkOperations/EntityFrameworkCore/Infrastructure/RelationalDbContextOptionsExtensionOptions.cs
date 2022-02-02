using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Options for bulk operations.
/// </summary>
public interface IBulkOperationsDbContextOptionsExtensionOptions : ISingletonOptions
{
   /// <summary>
   /// Indication whether to configure temp tables for primitive types.
   /// </summary>
   bool ConfigureTempTablesForPrimitiveTypes { get; }
}
