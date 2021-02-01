using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Cache key for <see cref="SqlServerTempTableCreator"/>.
   /// </summary>
   public readonly struct SqlServerTempTableCreatorCacheKey
      : IEquatable<SqlServerTempTableCreatorCacheKey>
   {
      /// <inheritdoc cref="ITempTableCreationOptions.TruncateTableIfExists" />
      public bool TruncateTableIfExists { get; }

      /// <inheritdoc cref="ISqlServerTempTableCreationOptions.UseDefaultDatabaseCollation" />
      public bool UseDefaultDatabaseCollation { get; }

      /// <summary>
      /// Properties to create temp table with.
      /// </summary>
      public IReadOnlyList<IProperty> Properties { get; }

      /// <summary>
      /// Properties the primary key should be created with.
      /// </summary>
      public IReadOnlyCollection<IProperty> PrimaryKeys { get; }

      /// <summary>
      /// Initializes new instance of <see cref="SqlServerTempTableCreatorCacheKey"/>.
      /// </summary>
      /// <param name="options">Options.</param>
      /// <param name="entityType">Entity type.</param>
      public SqlServerTempTableCreatorCacheKey(
         ISqlServerTempTableCreationOptions options,
         IEntityType entityType)
      {
         TruncateTableIfExists = options.TruncateTableIfExists;
         UseDefaultDatabaseCollation = options.UseDefaultDatabaseCollation;
         Properties = options.MembersToInclude.GetPropertiesForTempTable(entityType);
         PrimaryKeys = options.PrimaryKeyCreation.GetPrimaryKeyProperties(entityType, Properties);
      }

      /// <inheritdoc />
      public bool Equals(SqlServerTempTableCreatorCacheKey other)
      {
         return TruncateTableIfExists == other.TruncateTableIfExists &&
                UseDefaultDatabaseCollation == other.UseDefaultDatabaseCollation &&
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
         return obj is SqlServerTempTableCreatorCacheKey other && Equals(other);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         var hashCode = new HashCode();
         hashCode.Add(TruncateTableIfExists);
         hashCode.Add(UseDefaultDatabaseCollation);

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
