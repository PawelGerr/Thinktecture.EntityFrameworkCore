using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.TempTables.NameSuffixing;

internal class CachedTempTableSuffixes
{
   public Dictionary<IEntityType, TempTableSuffixes> SuffixLookup { get; }
   public int NumberOfConsumers { get; private set; }

   public CachedTempTableSuffixes()
   {
      SuffixLookup = new Dictionary<IEntityType, TempTableSuffixes>();
   }

   public void IncrementNumberOfConsumers()
   {
      NumberOfConsumers++;
   }

   public void DecrementNumberOfConsumers()
   {
      if (NumberOfConsumers == 0)
         throw new InvalidOperationException("The number of consumers is 0 already.");

      NumberOfConsumers--;
   }
}
