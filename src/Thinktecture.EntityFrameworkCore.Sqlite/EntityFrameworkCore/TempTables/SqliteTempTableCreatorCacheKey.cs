using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Cache key for <see cref="SqliteTempTableCreator"/>.
   /// </summary>
   public readonly struct SqliteTempTableCreatorCacheKey
      : IEquatable<SqliteTempTableCreatorCacheKey>
   {
      /// <inheritdoc cref="ITempTableCreationOptions.TruncateTableIfExists" />
      public bool TruncateTableIfExists { get; }

      /// <summary>
      /// Properties to create temp table with.
      /// </summary>
      public IReadOnlyList<IProperty> Properties { get; }

      /// <summary>
      /// Properties the primary key should be created with.
      /// </summary>
      public IReadOnlyCollection<IProperty> PrimaryKeys { get; }

      /// <summary>
      /// Initializes new instance of <see cref="SqliteTempTableCreatorCacheKey"/>.
      /// </summary>
      /// <param name="options">Options.</param>
      /// <param name="entityType">Entity type.</param>
      public SqliteTempTableCreatorCacheKey(
         ITempTableCreationOptions options,
         IEntityType entityType)
      {
         TruncateTableIfExists = options.TruncateTableIfExists;
         Properties = options.PropertiesToInclude.DeterminePropertiesForTempTable(entityType);
         PrimaryKeys = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, Properties);
      }

      /// <inheritdoc />
      public bool Equals(SqliteTempTableCreatorCacheKey other)
      {
         return TruncateTableIfExists == other.TruncateTableIfExists &&
                Equals(Properties, other.Properties) &&
                Equals(PrimaryKeys, other.PrimaryKeys);
      }

      private static bool Equals(IReadOnlyCollection<IProperty> collection, IReadOnlyCollection<IProperty> other)
      {
         if (collection.Count != other.Count)
            return false;

         using var otherEnumerator = other.GetEnumerator();

         foreach (var item in collection)
         {
            otherEnumerator.MoveNext();

            if (item != otherEnumerator.Current)
               return false;
         }

         return true;
      }

      /// <inheritdoc />
      public override bool Equals(object? obj)
      {
         return obj is SqliteTempTableCreatorCacheKey other && Equals(other);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         var hashCode = new HashCode();
         hashCode.Add(TruncateTableIfExists);

         ComputeHashCode(hashCode, Properties);
         ComputeHashCode(hashCode, PrimaryKeys);

         return hashCode.ToHashCode();
      }

      private static void ComputeHashCode(
         HashCode hashCode,
         IEnumerable<IProperty> properties)
      {
         foreach (var property in properties)
         {
            hashCode.Add(property);
         }
      }
   }
}
