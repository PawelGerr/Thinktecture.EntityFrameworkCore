using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Options for <see cref="NpgsqlDbContextOptionsExtension"/>.
/// </summary>
public class NpgsqlDbContextOptionsExtensionOptions : IBulkOperationsDbContextOptionsExtensionOptions
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
         throw new InvalidOperationException($"The setting '{nameof(NpgsqlDbContextOptionsExtension.ConfigureTempTablesForPrimitiveTypes)}' has been changed.");

      if (extension.ConfigureCollectionParametersForPrimitiveTypes != ConfigureCollectionParametersForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(NpgsqlDbContextOptionsExtension.ConfigureCollectionParametersForPrimitiveTypes)}' has been changed.");

      if (extension.UseDeferredCollectionParameterSerialization != UseDeferredCollectionParameterSerialization)
         throw new InvalidOperationException($"The setting '{nameof(NpgsqlDbContextOptionsExtension.UseDeferredCollectionParameterSerialization)}' has been changed.");
   }

   private static NpgsqlDbContextOptionsExtension GetExtension(IDbContextOptions options)
   {
      return options.FindExtension<NpgsqlDbContextOptionsExtension>()
             ?? throw new InvalidOperationException($"{nameof(NpgsqlDbContextOptionsExtension)} not found in current '{nameof(IDbContextOptions)}'.");
   }
}
