using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Singleton options of the MySQL extension: the conventions for primitive types and the insert strategy of the bulk operations (<see cref="UseBulkCopy" />, <see cref="MaxParametersPerStatement" />).
/// </summary>
public sealed class MySqlDbContextOptionsExtensionOptions : IBulkOperationsDbContextOptionsExtensionOptions
{
   /// <inheritdoc />
   public bool ConfigureTempTablesForPrimitiveTypes { get; private set; }

   /// <summary>
   /// Indicates whether primitive collection parameter entity types are configured by convention.
   /// </summary>
   public bool ConfigureCollectionParametersForPrimitiveTypes { get; private set; }

   /// <summary>
   /// Indicates whether the bulk operations insert with <see cref="MySqlConnector.MySqlBulkCopy" /> instead of multi-row INSERT statements.
   /// </summary>
   public bool UseBulkCopy { get; private set; }

   /// <summary>
   /// Maximum number of parameters of one multi-row INSERT statement of the bulk operations.
   /// </summary>
   public int MaxParametersPerStatement { get; private set; }

   /// <inheritdoc />
   public void Initialize(IDbContextOptions options)
   {
      var extension = GetExtension(options);
      ConfigureTempTablesForPrimitiveTypes = extension.ConfigureTempTablesForPrimitiveTypes;
      ConfigureCollectionParametersForPrimitiveTypes = extension.ConfigureCollectionParametersForPrimitiveTypes;
      UseBulkCopy = extension.UseBulkCopy;
      MaxParametersPerStatement = extension.MaxParametersPerStatement;
   }

   /// <inheritdoc />
   public void Validate(IDbContextOptions options)
   {
      if (GetExtension(options).ConfigureTempTablesForPrimitiveTypes != ConfigureTempTablesForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(MySqlDbContextOptionsExtension.ConfigureTempTablesForPrimitiveTypes)}' has been changed.");

      if (GetExtension(options).ConfigureCollectionParametersForPrimitiveTypes != ConfigureCollectionParametersForPrimitiveTypes)
         throw new InvalidOperationException($"The setting '{nameof(MySqlDbContextOptionsExtension.ConfigureCollectionParametersForPrimitiveTypes)}' has been changed.");

      if (GetExtension(options).UseBulkCopy != UseBulkCopy)
         throw new InvalidOperationException($"The setting '{nameof(MySqlDbContextOptionsExtension.UseBulkCopy)}' has been changed.");

      if (GetExtension(options).MaxParametersPerStatement != MaxParametersPerStatement)
         throw new InvalidOperationException($"The setting '{nameof(MySqlDbContextOptionsExtension.MaxParametersPerStatement)}' has been changed.");
   }

   private static MySqlDbContextOptionsExtension GetExtension(IDbContextOptions options)
   {
      return options.FindExtension<MySqlDbContextOptionsExtension>()
             ?? throw new InvalidOperationException($"{nameof(MySqlDbContextOptionsExtension)} not found in current '{nameof(IDbContextOptions)}'.");
   }
}