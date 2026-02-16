namespace Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;

internal class TempTableSuffixes
{
   private int _numberOfTempTables;
   private SortedSet<int>? _suffixes;

   // perf optimization
   private int? _firstSuffix;

   public TempTableSuffixLease Lease()
   {
      if (_suffixes?.Count > 0)
      {
         var suffix = _suffixes.First();
         _suffixes.Remove(suffix);
         return new TempTableSuffixLease(suffix, this);
      }

      if (_firstSuffix.HasValue)
      {
         var suffix = _firstSuffix.Value;
         _firstSuffix = null;
         return new TempTableSuffixLease(suffix, this);
      }

      _numberOfTempTables++;
      return new TempTableSuffixLease(_numberOfTempTables, this);
   }

   public void Return(int suffix)
   {
      if (_suffixes == null)
      {
         if (!_firstSuffix.HasValue)
         {
            _firstSuffix = suffix;
            return;
         }

         _suffixes = [_firstSuffix.Value];
         _firstSuffix = null;
      }

      if (!_suffixes.Add(suffix))
         throw new InvalidOperationException($"The suffix '{suffix}' is returned already.");
   }
}
