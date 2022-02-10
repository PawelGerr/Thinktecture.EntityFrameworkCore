using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Options for the <see cref="RelationalMethodCallTranslatorPlugin"/>.
/// </summary>
public class SqlServerDbContextOptionsExtensionOptions : IBulkOperationsDbContextOptionsExtensionOptions
{
   /// <inheritdoc />
   public bool ConfigureTempTablesForPrimitiveTypes { get; private set; }

   /// <summary>
   /// Indication whether to configure collection parameters for primitive types.
   /// </summary>
   public bool ConfigureCollectionParametersForPrimitiveTypes { get; private set; }

   /// <summary>
   /// Indication whether to use deferred serialization or not.
   /// </summary>
   public bool UseDeferredCollectionParameterSerialization { get; private set; }

   /// <inheritdoc />
   public void Initialize(IDbContextOptions options)
   {
      var extension = GetExtension(options);

      ConfigureTempTablesForPrimitiveTypes = extension.ConfigureTempTablesForPrimitiveTypes;
      ConfigureCollectionParametersForPrimitiveTypes = extension.ConfigureCollectionParametersForPrimitiveTypes;
      UseDeferredCollectionParameterSerialization = extension.UseDeferredCollectionParameterSerialization;
   }

   /// <inheritdoc />
   public void Validate(IDbContextOptions options)
   {
      var extension = GetExtension(options);

      if (extension.ConfigureTempTablesForPrimitiveTypes != ConfigureTempTablesForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(SqlServerDbContextOptionsExtension.ConfigureTempTablesForPrimitiveTypes)}' has been changed.");

      if (extension.ConfigureCollectionParametersForPrimitiveTypes != ConfigureCollectionParametersForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(SqlServerDbContextOptionsExtension.ConfigureCollectionParametersForPrimitiveTypes)}' has been changed.");

      if (extension.UseDeferredCollectionParameterSerialization != UseDeferredCollectionParameterSerialization)
         throw new InvalidOperationException($"The setting '{nameof(SqlServerDbContextOptionsExtension.UseDeferredCollectionParameterSerialization)}' has been changed.");
   }

   private static SqlServerDbContextOptionsExtension GetExtension(IDbContextOptions options)
   {
      return options.FindExtension<SqlServerDbContextOptionsExtension>()
             ?? throw new InvalidOperationException($"{nameof(SqlServerDbContextOptionsExtension)} not found in current '{nameof(IDbContextOptions)}'.");
   }
}
