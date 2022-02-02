using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Conventions for bulk operations.
/// </summary>
public class BulkOperationConventionSetPlugin : IConventionSetPlugin
{
   private readonly IBulkOperationsDbContextOptionsExtensionOptions _options;

   /// <summary>
   /// Initializes a new instance of <see cref="BulkOperationConventionSetPlugin"/>.
   /// </summary>
   public BulkOperationConventionSetPlugin(IBulkOperationsDbContextOptionsExtensionOptions options)
   {
      _options = options ?? throw new ArgumentNullException(nameof(options));
   }

   /// <inheritdoc />
   public ConventionSet ModifyConventions(ConventionSet conventionSet)
   {
      if (_options.ConfigureTempTablesForPrimitiveTypes)
         conventionSet.ModelInitializedConventions.Add(TempTableConvention.Instance);

      return conventionSet;
   }
}
