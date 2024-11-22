using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Provides the properties to be used for creation of the primary key.
/// </summary>
public interface IPrimaryKeyPropertiesProvider
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
      ArgumentNullException.ThrowIfNull(projection);

      var members = projection.ExtractMembers();

      if (members.Count == 0)
         throw new ArgumentException("The provided projection contains no properties.");

      return new KeyPropertiesProvider(members);
   }

   /// <summary>
   /// Gets the primary key properties.
   /// </summary>
   /// <param name="entityType">Entity type to get the primary key properties for.</param>
   /// <param name="tempTableProperties">Actual properties of the temp table.</param>
   /// <returns>Properties to use for creation of the primary key.</returns>
   IReadOnlyCollection<IProperty> GetPrimaryKeyProperties(IEntityType entityType, IReadOnlyCollection<IProperty> tempTableProperties);
}
