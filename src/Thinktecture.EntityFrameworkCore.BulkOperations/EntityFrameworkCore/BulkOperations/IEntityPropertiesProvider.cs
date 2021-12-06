using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Provides entity properties to work with.
/// </summary>
public interface IEntityPropertiesProvider
{
   /// <summary>
   /// Determines properties to include into a temp table into.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
   /// <param name="filter">Filter.</param>
   /// <returns>Properties to include into a temp table.</returns>
   IReadOnlyList<PropertyWithNavigations> GetPropertiesForTempTable(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);

   /// <summary>
   /// Determines properties to include into a temp table into.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
   /// <param name="filter">Filter.</param>
   /// <returns>Properties to include into a temp table.</returns>
   IReadOnlyList<PropertyWithNavigations> GetKeyProperties(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);

   /// <summary>
   /// Determines properties to insert into a (temp) table.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
   /// <param name="filter">Filter.</param>
   /// <returns>Properties to insert into a (temp) table.</returns>
   IReadOnlyList<PropertyWithNavigations> GetPropertiesForInsert(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);

   /// <summary>
   /// Determines properties to use in update of a table.
   /// </summary>
   /// <param name="entityType">Entity type.</param>
   /// <param name="inlinedOwnTypes">Indication whether inlined (<c>true</c>), separated (<c>false</c>) or all owned types to return.</param>
   /// <param name="filter">Filter.</param>
   /// <returns>Properties to use in update of a table.</returns>
   IReadOnlyList<PropertyWithNavigations> GetPropertiesForUpdate(
      IEntityType entityType,
      bool? inlinedOwnTypes,
      Func<IProperty, IReadOnlyList<INavigation>, bool> filter);
}
