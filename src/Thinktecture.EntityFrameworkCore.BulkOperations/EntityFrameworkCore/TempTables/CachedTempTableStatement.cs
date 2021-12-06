using System;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cached SQL statement for creation of temp tables.
/// </summary>
public readonly struct CachedTempTableStatement
{
   private readonly Func<string, string> _statementProvider;

   /// <summary>
   /// Initializes new instance of <see cref="CachedTempTableStatement"/>.
   /// </summary>
   /// <param name="statementProvider">Delegate for getting a SQL statement for provided table name.</param>
   public CachedTempTableStatement(Func<string, string> statementProvider)
   {
      _statementProvider = statementProvider ?? throw new ArgumentNullException(nameof(statementProvider));
   }

   /// <summary>
   /// Get a SQL statement for provided <paramref name="tableName"/>.
   /// </summary>
   /// <param name="tableName">The name of the temp table.</param>
   /// <returns>SQL statement for creation of a temp table.</returns>
   public string GetSqlStatement(string tableName)
   {
      return _statementProvider(tableName);
   }
}