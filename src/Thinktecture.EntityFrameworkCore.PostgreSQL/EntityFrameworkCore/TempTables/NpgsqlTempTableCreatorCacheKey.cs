using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cache key for <see cref="NpgsqlTempTableCreator"/>.
/// </summary>
public readonly struct NpgsqlTempTableCreatorCacheKey
   : IEquatable<NpgsqlTempTableCreatorCacheKey>
{
   /// <inheritdoc cref="ITempTableCreationOptions.TruncateTableIfExists" />
   public bool TruncateTableIfExists { get; }

   /// <inheritdoc cref="NpgsqlTempTableCreationOptions.SplitCollationComponents" />
   public bool SplitCollationComponents { get; }

   /// <summary>
   /// Properties to create temp table with.
   /// </summary>
   public IReadOnlyList<IProperty> Properties { get; }

   /// <summary>
   /// Properties the primary key should be created with.
   /// </summary>
   public IReadOnlyCollection<IProperty> PrimaryKeys { get; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlTempTableCreatorCacheKey"/>.
   /// </summary>
   /// <param name="options">Options.</param>
   /// <param name="entityType">Entity type.</param>
   public NpgsqlTempTableCreatorCacheKey(
      NpgsqlTempTableCreationOptions options,
      IEntityType entityType)
   {
      TruncateTableIfExists = options.TruncateTableIfExists;
      SplitCollationComponents = options.SplitCollationComponents;
      Properties = options.PropertiesToInclude.DeterminePropertiesForTempTable(entityType);
      PrimaryKeys = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, Properties);
   }

   /// <inheritdoc />
   public bool Equals(NpgsqlTempTableCreatorCacheKey other)
   {
      return TruncateTableIfExists == other.TruncateTableIfExists &&
             SplitCollationComponents == other.SplitCollationComponents &&
             Properties.AreEqual(other.Properties) &&
             PrimaryKeys.AreEqual(other.PrimaryKeys);
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj is NpgsqlTempTableCreatorCacheKey other && Equals(other);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hashCode = new HashCode();
      hashCode.Add(TruncateTableIfExists);
      hashCode.Add(SplitCollationComponents);

      Properties.ComputeHashCode(hashCode);
      PrimaryKeys.ComputeHashCode(hashCode);

      return hashCode.ToHashCode();
   }
}
