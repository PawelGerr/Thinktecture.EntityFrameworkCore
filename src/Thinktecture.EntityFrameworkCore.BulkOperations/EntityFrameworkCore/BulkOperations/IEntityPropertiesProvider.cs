using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides entity properties to work with.
/// </summary>
public interface IEntityPropertiesProvider
{
   /// <summary>
   /// An <see cref="IEntityPropertiesProvider"/> with 0 properties.
   /// </summary>
   public static readonly IEntityPropertiesProvider Empty = new IncludingEntityPropertiesProvider(Array.Empty<MemberInfo>());

   /// <summary>
   /// An <see cref="IEntityPropertiesProvider"/> with all properties of an entity.
   /// </summary>
   public static readonly IEntityPropertiesProvider Default = new DefaultPropertiesEntityPropertiesProvider();

   /// <summary>
   /// Creates a new <see cref="IEntityPropertiesProvider"/> with specified <paramref name="members"/>.
   /// </summary>
   /// <param name="members">Members to create the provider for.</param>
   /// <returns>A new instance of <see cref="IEntityPropertiesProvider"/>.</returns>
   public static IEntityPropertiesProvider Include(IReadOnlyList<MemberInfo> members)
   {
      return new IncludingEntityPropertiesProvider(members);
   }

   /// <summary>
   /// Extracts members from the provided <paramref name="projection"/> and creates an <see cref="IEntityPropertiesProvider"/>
   /// which provides the specified members to the callers.
   /// </summary>
   /// <param name="projection">Projection to extract the members from.</param>
   /// <typeparam name="T">Type of the entity.</typeparam>
   /// <returns>An instance of <see cref="IEntityPropertiesProvider"/> containing members extracted from <paramref name="projection"/>.</returns>
   /// <exception cref="ArgumentNullException"><paramref name="projection"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">No members couldn't be extracted.</exception>
   /// <exception cref="NotSupportedException">The <paramref name="projection"/> contains unsupported expressions.</exception>
   public static IEntityPropertiesProvider Include<T>(Expression<Func<T, object?>> projection)
   {
      ArgumentNullException.ThrowIfNull(projection);

      var members = projection.ExtractMembers();

      return members.Count == 0 ? Empty : new IncludingEntityPropertiesProvider(members);
   }

   /// <summary>
   /// Extracts members from the provided <paramref name="projection"/> and creates an <see cref="IEntityPropertiesProvider"/>
   /// which provides all properties of the corresponding entity besides the specified members.
   /// </summary>
   /// <param name="projection"></param>
   /// <typeparam name="T"></typeparam>
   /// <returns></returns>
   public static IEntityPropertiesProvider Exclude<T>(Expression<Func<T, object?>> projection)
   {
      ArgumentNullException.ThrowIfNull(projection);

      var members = projection.ExtractMembers();

      return members.Count == 0 ? Default : new ExcludingEntityPropertiesProvider(members);
   }

   /// <summary>
   /// Determines properties to include into a temp table into.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <returns>Properties to include into a temp table.</returns>
   IReadOnlyList<IProperty> GetPropertiesForTempTable(IEntityType entityType);

   /// <summary>
   /// Determines properties to include into a temp table into.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <returns>Properties to include into a temp table.</returns>
   IReadOnlyList<IProperty> GetKeyProperties(IEntityType entityType);

   /// <summary>
   /// Determines properties to insert into a (temp) table.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
   /// <returns>Properties to insert into a (temp) table.</returns>
   IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(IEntityType entityType, bool? inlinedOwnTypes);

   /// <summary>
   /// Determines properties to use in update of a table.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
   /// <returns>Properties to use in update of a table.</returns>
   IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(IEntityType entityType, bool? inlinedOwnTypes);

   internal static bool InsertAndUpdateFilter(IProperty property, IReadOnlyList<INavigation> navigations)
   {
      return property.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore &&
             (navigations.Count == 0 || !navigations[^1].IsInlined() || !property.IsKey());
   }
}
