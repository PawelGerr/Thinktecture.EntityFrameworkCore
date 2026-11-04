using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Singleton options used by MySQL bulk-operation conventions.
/// </summary>
public sealed class MySqlDbContextOptionsExtensionOptions : IBulkOperationsDbContextOptionsExtensionOptions
{
   /// <inheritdoc />
   public bool ConfigureTempTablesForPrimitiveTypes { get; private set; }

   /// <summary>
   /// Indicates whether primitive collection parameter entity types are configured by convention.
   /// </summary>
   public bool ConfigureCollectionParametersForPrimitiveTypes { get; private set; }

   /// <inheritdoc />
   public void Initialize(IDbContextOptions options)
   {
      ConfigureTempTablesForPrimitiveTypes = GetExtension(options).ConfigureTempTablesForPrimitiveTypes;
      ConfigureCollectionParametersForPrimitiveTypes = GetExtension(options).ConfigureCollectionParametersForPrimitiveTypes;
   }

   /// <inheritdoc />
   public void Validate(IDbContextOptions options)
   {
      if (GetExtension(options).ConfigureTempTablesForPrimitiveTypes != ConfigureTempTablesForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(MySqlDbContextOptionsExtension.ConfigureTempTablesForPrimitiveTypes)}' has been changed.");

      if (GetExtension(options).ConfigureCollectionParametersForPrimitiveTypes != ConfigureCollectionParametersForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(MySqlDbContextOptionsExtension.ConfigureCollectionParametersForPrimitiveTypes)}' has been changed.");
   }

   private static MySqlDbContextOptionsExtension GetExtension(IDbContextOptions options)
   {
      return options.FindExtension<MySqlDbContextOptionsExtension>()
             ?? throw new InvalidOperationException($"{nameof(MySqlDbContextOptionsExtension)} not found in current '{nameof(IDbContextOptions)}'.");
   }
}