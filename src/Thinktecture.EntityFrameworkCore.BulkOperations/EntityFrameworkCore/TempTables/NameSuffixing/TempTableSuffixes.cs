using System;
using System.Collections.Generic;
using System.Linq;

namespace Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;

internal class TempTableSuffixes
{
   private int _numberOfTempTables;
   private SortedSet<int>? _suffixes;

   public TempTableSuffixLease Lease()
   {
      if (_suffixes?.Count > 0)
      {
         var suffix = _suffixes.First();
         _suffixes.Remove(suffix);
         return new TempTableSuffixLease(suffix, this);
      }

      _numberOfTempTables++;
      return new TempTableSuffixLease(_numberOfTempTables, this);
   }

   public void Return(int suffix)
   {
      if (_suffixes == null)
      {
         _suffixes = new SortedSet<int>();
      }
      else if (_suffixes.Contains(suffix))
      {
         throw new InvalidOperationException($"The suffix '{suffix}' is returned already.");
      }

      _suffixes.Add(suffix);
   }
}