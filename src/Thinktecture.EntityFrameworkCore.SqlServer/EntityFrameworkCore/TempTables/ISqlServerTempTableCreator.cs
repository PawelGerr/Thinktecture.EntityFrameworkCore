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
      ///
      /// <remarks>
      /// If the type <paramref name="entityType"/> is a keyless type then all columns are part of the primary key
      /// because a query type has no "Primary Key" by definition.
      /// </remarks>
      /// </summary>
      /// <param name="ctx">Database context to use.</param>
      /// <param name="entityType">Entity type.</param>
      /// <param name="tableName">Table name to create the primary key in.</param>
      /// <param name="checkForExistence">If <c>true</c> then the primary key is not going to be created if it exists already.</param>
      /// <param name="cancellationToken">Cancellation token.</param>
      Task CreatePrimaryKeyAsync(DbContext ctx, IEntityType entityType, string tableName, bool checkForExistence = false, CancellationToken cancellationToken = default);
   }
}
