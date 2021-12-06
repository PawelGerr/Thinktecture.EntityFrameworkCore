using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Contains some predefined implementations of <see cref="IPrimaryKeyPropertiesProvider"/>.
/// </summary>
public static class PrimaryKeyPropertiesProviders
{
   /// <summary>
   /// Provides no properties, i.e. no primary key will be created.
   /// </summary>
   public static readonly IPrimaryKeyPropertiesProvider None = new NoPrimaryKeyPropertiesProvider();

   /// <summary>
   /// Provides the primary key properties configured for the corresponding <see cref="IEntityType"/>.
   /// If the entity is keyless then no primary key is created.
   /// </summary>
   /// <exception cref="ArgumentException">Is thrown when not all key properties are part of the current temp table.</exception>
   public static readonly IPrimaryKeyPropertiesProvider EntityTypeConfiguration = new ConfiguredPrimaryKeyPropertiesProvider();

   /// <summary>
   /// Provides the primary key properties configured for the corresponding <see cref="IEntityType"/>.
   /// If the entity is keyless then no primary key is created.
   /// Columns which are not part of the actual temp table are skipped.
   /// </summary>
   public static readonly IPrimaryKeyPropertiesProvider AdaptiveEntityTypeConfiguration = new AdaptiveEntityTypeConfigurationPrimaryKeyPropertiesProvider();

   /// <summary>
   /// Provides the primary key properties configured for the corresponding <see cref="IEntityType"/>.
   /// If the entity is keyless then all its properties are used for creation of the primary key.
   /// Properties which are not part of the actual temp table are skipped.
   /// </summary>
   public static readonly IPrimaryKeyPropertiesProvider AdaptiveForced = new AdaptiveForcedPrimaryKeyPropertiesProvider();

   /// <summary>
   /// Extracts members from the provided <paramref name="projection"/>.
   /// </summary>
   /// <param name="projection">Projection to extract the members from.</param>
   /// <typeparam name="T">Type of the entity.</typeparam>
   /// <returns>An instance of <see cref="IPrimaryKeyPropertiesProvider"/> containing members extracted from <paramref name="projection"/>.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">No members couldn't be extracted.</exception>
   /// <exception cref="NotSupportedException">The <paramref name="projection"/> contains unsupported expressions.</exception>
   public static IPrimaryKeyPropertiesProvider From<T>(Expression<Func<T, object?>> projection)
   {
      if (projection == null)
         throw new ArgumentNullException(nameof(projection));

      var members = projection.ExtractMembers();

      if (members.Count == 0)
         throw new ArgumentException("The provided projection contains no properties.");

      return new KeyPropertiesProvider(members);
   }

   private sealed class NoPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
   {
      public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (tempTableProperties == null)
            throw new ArgumentNullException(nameof(tempTableProperties));

         return Array.Empty<PropertyWithNavigations>();
      }
   }

   private sealed class ConfiguredPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
   {
      public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
      {
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));
         if (tempTableProperties == null)
            throw new ArgumentNullException(nameof(tempTableProperties));

         var pk = entityType.FindPrimaryKey()?.Properties;

         if (pk is null or { Count: 0 })
            return Array.Empty<PropertyWithNavigations>();

         var keyProperties = pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
         var missingColumns = keyProperties.Except(tempTableProperties);

         if (missingColumns.Any())
         {
            throw new ArgumentException(@$"Cannot create PRIMARY KEY because not all key columns are part of the temp table.
You may use other key properties providers like '{nameof(PrimaryKeyPropertiesProviders)}.{nameof(AdaptiveEntityTypeConfiguration)}' instead of '{nameof(PrimaryKeyPropertiesProviders)}.{nameof(EntityTypeConfiguration)}' to get different behaviors.
Missing columns: {String.Join(", ", missingColumns.Select(p => p.Property.GetColumnBaseName()))}.");
         }

         return keyProperties;
      }
   }

   private sealed class AdaptiveEntityTypeConfigurationPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
   {
      public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
      {
         if (tempTableProperties.Count == 0)
            return Array.Empty<PropertyWithNavigations>();

         var pk = entityType.FindPrimaryKey()?.Properties;

         if (pk is null or { Count: 0 })
            return Array.Empty<PropertyWithNavigations>();

         return pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>()))
                  .Intersect(tempTableProperties)
                  .ToList();
      }
   }

   private sealed class AdaptiveForcedPrimaryKeyPropertiesProvider : IPrimaryKeyPropertiesProvider
   {
      public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
      {
         if (tempTableProperties.Count == 0)
            return Array.Empty<PropertyWithNavigations>();

         var pk = entityType.FindPrimaryKey()?.Properties;

         if (pk is null or { Count: 0 })
            return tempTableProperties;

         return pk.Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>()))
                  .Intersect(tempTableProperties).ToList();
      }
   }

   private class KeyPropertiesProvider : IPrimaryKeyPropertiesProvider
   {
      private readonly IReadOnlyList<MemberInfo> _members;

      public KeyPropertiesProvider(IReadOnlyList<MemberInfo> members)
      {
         _members = members;
      }

      public IReadOnlyCollection<PropertyWithNavigations> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<PropertyWithNavigations> tempTableProperties)
      {
         var keyProperties = _members.ConvertToEntityProperties(entityType, true, NoFilter);
         var missingColumns = keyProperties.Except(tempTableProperties);

         if (missingColumns.Any())
         {
            throw new ArgumentException(@$"Not all key columns are part of the table.
Missing columns: {String.Join(", ", missingColumns.Select(p => p.Property.GetColumnBaseName()))}.");
         }

         return keyProperties;
      }

      private static bool NoFilter(IProperty property, IReadOnlyList<INavigation> navigations)
      {
         return true;
      }
   }
}
