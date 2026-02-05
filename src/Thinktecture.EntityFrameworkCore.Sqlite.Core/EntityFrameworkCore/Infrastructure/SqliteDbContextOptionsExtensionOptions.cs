using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Options for the <see cref="RelationalMethodCallTranslatorPlugin"/>.
/// </summary>
public class SqliteDbContextOptionsExtensionOptions : IBulkOperationsDbContextOptionsExtensionOptions
{
   /// <inheritdoc />
   public bool ConfigureTempTablesForPrimitiveTypes { get; private set; }

   /// <inheritdoc />
   public void Initialize(IDbContextOptions options)
   {
      var extension = GetExtension(options);

      ConfigureTempTablesForPrimitiveTypes = extension.ConfigureTempTablesForPrimitiveTypes;
   }

   /// <inheritdoc />
   public void Validate(IDbContextOptions options)
   {
      var extension = GetExtension(options);

      if (extension.ConfigureTempTablesForPrimitiveTypes != ConfigureTempTablesForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(SqliteDbContextOptionsExtension.ConfigureTempTablesForPrimitiveTypes)}' has been changed.");
   }

   private static SqliteDbContextOptionsExtension GetExtension(IDbContextOptions options)
   {
      return options.FindExtension<SqliteDbContextOptionsExtension>()
             ?? throw new InvalidOperationException($"{nameof(SqliteDbContextOptionsExtension)} not found in current '{nameof(IDbContextOptions)}'.");
   }
}
