using System;
using System.Collections.Concurrent;

namespace Thinktecture.EntityFrameworkCore.TempTables;

/// <summary>
/// Cache for SQL statements for creation of temp tables.
/// </summary>
/// <typeparam name="TCacheKey">The type of the cache key.</typeparam>
public class TempTableStatementCache<TCacheKey>
   where TCacheKey : IEquatable<TCacheKey>
{
   private readonly ConcurrentDictionary<TCacheKey, CachedTempTableStatement> _cache = new();

   /// <summary>
   /// Gets cached SQL statement for creation of a temp table or creates a new one and caches it.
   /// </summary>
   /// <param name="cacheKey">The cache key.</param>
   /// <param name="factory">Factory for creation of a new SQL statement for creation of a temp table if the statement for provided <paramref name="cacheKey"/> is missing.</param>
   /// <returns>Cached SQL statement.</returns>
   public CachedTempTableStatement GetOrAdd(TCacheKey cacheKey, Func<TCacheKey, CachedTempTableStatement> factory)
   {
      return _cache.GetOrAdd(cacheKey, factory);
   }
}