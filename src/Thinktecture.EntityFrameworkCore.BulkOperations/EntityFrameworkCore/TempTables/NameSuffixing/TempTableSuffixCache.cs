using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;
#pragma warning disable CA1812

internal class TempTableSuffixCache
{
   private readonly object _lock = new();
   private readonly Dictionary<DbConnection, CachedTempTableSuffixes> _globalCache;

   public TempTableSuffixCache()
   {
      _globalCache = new Dictionary<DbConnection, CachedTempTableSuffixes>();
   }

   public Dictionary<IEntityType, TempTableSuffixes> LeaseSuffixLookup(
      DbConnection connection)
   {
      if (connection == null)
         throw new ArgumentNullException(nameof(connection));

      lock (_lock)
      {
         if (!_globalCache.TryGetValue(connection, out var cachedSuffixes))
         {
            cachedSuffixes = new CachedTempTableSuffixes();
            _globalCache.Add(connection, cachedSuffixes);
         }

         cachedSuffixes.IncrementNumberOfConsumers();

         return cachedSuffixes.SuffixLookup;
      }
   }

   public void ReturnSuffixLookup(
      DbConnection connection)
   {
      if (connection == null)
         throw new ArgumentNullException(nameof(connection));

      lock (_lock)
      {
         if (!_globalCache.TryGetValue(connection, out var cachedSuffixes))
            return;

         cachedSuffixes.DecrementNumberOfConsumers();

         if (cachedSuffixes.NumberOfConsumers == 0)
            _globalCache.Remove(connection);
      }
   }
}