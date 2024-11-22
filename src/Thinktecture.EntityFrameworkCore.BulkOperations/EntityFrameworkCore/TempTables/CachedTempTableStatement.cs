using Microsoft.EntityFrameworkCore.Storage;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cached SQL statement for creation of temp tables.
/// </summary>
public class CachedTempTableStatement<T> : ICachedTempTableStatement
{
   private readonly Func<ISqlGenerationHelper, string, T, string> _statementProvider;
   private readonly T _parameter;

   /// <summary>
   /// Initializes new instance of <see cref="CachedTempTableStatement{T}"/>
   /// </summary>
   /// <param name="parameter"></param>
   /// <param name="statementProvider"></param>
   public CachedTempTableStatement(T parameter, Func<ISqlGenerationHelper, string, T, string> statementProvider)
   {
      _statementProvider = statementProvider;
      _parameter = parameter;
   }

   /// <inheritdoc />
   public string GetSqlStatement(ISqlGenerationHelper sqlGenerationHelper, string tableName)
   {
      return _statementProvider(sqlGenerationHelper, tableName, _parameter);
   }
}
