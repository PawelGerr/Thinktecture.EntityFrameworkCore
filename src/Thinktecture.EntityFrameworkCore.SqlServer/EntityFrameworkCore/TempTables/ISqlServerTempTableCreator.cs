using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Creates temp tables for SQL server.
   /// </summary>
   public interface ISqlServerTempTableCreator : ITempTableCreator
   {
      /// <summary>
      /// Creates a primary key in a temp table with provided <paramref name="tableName"/>.
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="keyProperties">Properties which should be part of the primary key.</param>
      /// <param name="tableName">Table name to create the primary key in.</param>
      /// <param name="checkForExistence">If <c>true</c> then the primary key is not going to be created if it exists already.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      Task CreatePrimaryKeyAsync(DbContext ctx, IReadOnlyCollection<IProperty> keyProperties, string tableName, bool checkForExistence = false, CancellationToken cancellationToken = default);
   }
}
