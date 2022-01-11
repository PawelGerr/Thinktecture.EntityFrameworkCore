using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cached SQL statement for creation of temp tables.
/// </summary>
public interface ICachedTempTableStatement
{
   /// <summary>
   /// Get a SQL statement for provided <paramref name="tableName"/>.
   /// </summary>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="tableName">The name of the temp table.</param>
   /// <returns>SQL statement for creation of a temp table.</returns>
   string GetSqlStatement(ISqlGenerationHelper sqlGenerationHelper, string tableName);
}
