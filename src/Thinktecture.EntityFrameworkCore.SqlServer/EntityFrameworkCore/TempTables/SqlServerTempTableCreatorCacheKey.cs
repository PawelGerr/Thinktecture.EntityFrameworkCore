using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cache key for <see cref="SqlServerTempTableCreator"/>.
/// </summary>
public readonly struct SqlServerTempTableCreatorCacheKey
   : IEquatable<SqlServerTempTableCreatorCacheKey>
{
   /// <inheritdoc cref="ITempTableCreationOptions.TruncateTableIfExists" />
   public bool TruncateTableIfExists { get; }

   /// <inheritdoc cref="SqlServerTempTableCreationOptions.UseDefaultDatabaseCollation" />
   public bool UseDefaultDatabaseCollation { get; }

   /// <summary>
   /// Properties to create temp table with.
   /// </summary>
   public IReadOnlyList<PropertyWithNavigations> Properties { get; }

   /// <summary>
   /// Properties the primary key should be created with.
   /// </summary>
   public IReadOnlyCollection<PropertyWithNavigations> PrimaryKeys { get; }

   /// <summary>
   /// Do not use default values when we create a temp dable
   /// </summary>
   public bool DoNotUseDefaultValues { get; }

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerTempTableCreatorCacheKey"/>.
   /// </summary>
   /// <param name="options">Options.</param>
   /// <param name="entityType">Entity type.</param>
   public SqlServerTempTableCreatorCacheKey(
      SqlServerTempTableCreationOptions options,
      IEntityType entityType)
   {
      TruncateTableIfExists = options.TruncateTableIfExists;
      UseDefaultDatabaseCollation = options.UseDefaultDatabaseCollation;
      Properties = options.PropertiesToInclude.DeterminePropertiesForTempTable(entityType, true);
      PrimaryKeys = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, Properties);
      DoNotUseDefaultValues = options.DoNotUseDefaultValues;
   }

   /// <inheritdoc />
   public bool Equals(SqlServerTempTableCreatorCacheKey other)
   {
      return TruncateTableIfExists == other.TruncateTableIfExists &&
             UseDefaultDatabaseCollation == other.UseDefaultDatabaseCollation &&
             Properties.AreEqual(other.Properties) &&
             PrimaryKeys.AreEqual(other.PrimaryKeys)
             && DoNotUseDefaultValues == other.DoNotUseDefaultValues
             ;
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj is SqlServerTempTableCreatorCacheKey other && Equals(other);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hashCode = new HashCode();
      hashCode.Add(TruncateTableIfExists);
      hashCode.Add(UseDefaultDatabaseCollation);

      Properties.ComputeHashCode(hashCode);
      PrimaryKeys.ComputeHashCode(hashCode);

      hashCode.Add(DoNotUseDefaultValues);

      return hashCode.ToHashCode();
   }
}
