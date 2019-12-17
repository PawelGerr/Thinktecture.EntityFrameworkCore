using System;

namespace Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing
{
   internal readonly struct TempTableSuffixLease : IDisposable
   {
      public int Suffix { get; }
      private readonly TempTableSuffixes _suffixes;

      public TempTableSuffixLease(int suffix, TempTableSuffixes suffixes)
      {
         Suffix = suffix;
         _suffixes = suffixes ?? throw new ArgumentNullException(nameof(suffixes));
      }

      public void Dispose()
      {
         _suffixes.Return(Suffix);
      }
   }
}
