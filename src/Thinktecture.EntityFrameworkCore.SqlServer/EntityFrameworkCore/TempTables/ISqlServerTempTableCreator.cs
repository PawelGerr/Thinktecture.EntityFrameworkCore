using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Creates temp tables for SQL server.
/// </summary>
public interface ISqlServerTempTableCreator : ITempTableCreator
{
   /// <summary>
   /// Creates a temp table.
   /// </summary>
   /// <param name="entityType">Entity/query type.</param>
   /// <param name="options">Options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>A reference to a temp table.</returns>
   /// <exception cref="ArgumentNullException">
   /// <paramref name="entityType"/> is <c>null</c>.
   /// </exception>
   /// <exception cref="ArgumentException">The provided type <paramref name="entityType"/> is not known by the current <see cref="DbContext"/>.</exception>
   Task<ITempTableReference> CreateTempTableAsync(
      IEntityType entityType,
      ISqlServerTempTableCreationOptions options,
      CancellationToken cancellationToken = default);

   /// <summary>
   /// Creates a primary key in a temp table with provided <paramref name="tableName"/>.
   /// </summary>
   /// <param name="ctx">Database context to use.</param>
   /// <param name="keyProperties">Properties which should be part of the primary key.</param>
   /// <param name="tableName">Table name to create the primary key in.</param>
   /// <param name="checkForExistence">If <c>true</c> then the primary key is not going to be created if it exists already.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task CreatePrimaryKeyAsync(DbContext ctx, IReadOnlyCollection<PropertyWithNavigations> keyProperties, string tableName, bool checkForExistence = false, CancellationToken cancellationToken = default);
}