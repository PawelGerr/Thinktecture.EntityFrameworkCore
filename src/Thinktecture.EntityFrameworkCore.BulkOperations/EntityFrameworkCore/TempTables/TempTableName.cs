using System;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   internal class TempTableName : ITempTableNameLease
   {
      public string Name { get; }

      public TempTableName(string name)
      {
         Name = name ?? throw new ArgumentNullException(nameof(name));
      }

      public void Dispose()
      {
         // Nothing to dispose.
      }
   }
}
