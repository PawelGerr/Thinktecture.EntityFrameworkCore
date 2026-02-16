using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cache key for <see cref="NpgsqlTempTableCreator"/>.
/// </summary>
public readonly struct NpgsqlTempTablePrimaryKeyCacheKey
   : IEquatable<NpgsqlTempTablePrimaryKeyCacheKey>
{
   /// <summary>
   /// Properties to create the primary key with.
   /// </summary>
   public IReadOnlyCollection<IProperty> KeyProperties { get; }

   /// <summary>
   /// Indication whether to check for existence of PK.
   /// </summary>
   public bool CheckForExistence { get; }

   /// <summary>
   /// Initializes new instance of <see cref="NpgsqlTempTablePrimaryKeyCacheKey"/>.
   /// </summary>
   /// <param name="keyProperties">Properties to create the primary key with.</param>
   /// <param name="checkForExistence">Indication whether to check for existence of PK.</param>
   public NpgsqlTempTablePrimaryKeyCacheKey(
      IReadOnlyCollection<IProperty> keyProperties,
      bool checkForExistence)
   {
      KeyProperties = keyProperties;
      CheckForExistence = checkForExistence;
   }

   /// <inheritdoc />
   public bool Equals(NpgsqlTempTablePrimaryKeyCacheKey other)
   {
      return CheckForExistence == other.CheckForExistence &&
             KeyProperties.AreEqual(other.KeyProperties);
   }

   /// <inheritdoc />
   public override bool Equals(object? obj)
   {
      return obj is NpgsqlTempTablePrimaryKeyCacheKey other && Equals(other);
   }

   /// <inheritdoc />
   public override int GetHashCode()
   {
      var hashCode = new HashCode();
      hashCode.Add(CheckForExistence);

      KeyProperties.ComputeHashCode(hashCode);

      return hashCode.ToHashCode();
   }
}
