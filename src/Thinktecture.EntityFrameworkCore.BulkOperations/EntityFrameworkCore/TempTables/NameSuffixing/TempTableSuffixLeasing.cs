using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;
#pragma warning disable CA1812

internal class TempTableSuffixLeasing : IDisposable
{
   private readonly object _lock;
   private readonly TempTableSuffixCache _cache;
   private readonly ICurrentDbContext _currentDbContext;

#pragma warning disable CA2213
   private DbConnection? _connection;
#pragma warning restore CA2213

   private Dictionary<IEntityType, TempTableSuffixes>? _lookup;
   private bool _idDisposed;

   public TempTableSuffixLeasing(
      ICurrentDbContext currentDbContext,
      TempTableSuffixCache cache)
   {
      _cache = cache ?? throw new ArgumentNullException(nameof(cache));
      _currentDbContext = currentDbContext ?? throw new ArgumentNullException(nameof(currentDbContext));
      _lock = new object();

      // don't fetch the connection and suffix lookup immediately but on first use only
   }

   public TempTableSuffixLease Lease(IEntityType entityType)
   {
      if (entityType == null)
         throw new ArgumentNullException(nameof(entityType));

      EnsureDisposed();

      if (_lookup == null)
      {
         lock (_lock)
         {
            EnsureDisposed();

            if (_lookup == null)
            {
               _connection = _currentDbContext.Context.Database.GetDbConnection();
               _lookup = _cache.LeaseSuffixLookup(_connection);
            }
         }
      }

      if (!_lookup.TryGetValue(entityType, out var suffixes))
      {
         suffixes = new TempTableSuffixes();
         _lookup.Add(entityType, suffixes);
      }

      return suffixes.Lease();
   }

   private void EnsureDisposed()
   {
      if (_idDisposed)
         throw new ObjectDisposedException(nameof(TempTableSuffixLease));
   }

   public void Dispose()
   {
      if (_idDisposed)
         return;

      _idDisposed = true;

      lock (_lock)
      {
         if (_connection == null)
            return;

         _cache.ReturnSuffixLookup(_connection);
         _lookup = null;
         _connection = null;
      }
   }
}
