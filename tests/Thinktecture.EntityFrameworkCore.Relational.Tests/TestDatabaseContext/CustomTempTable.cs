using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.TestDatabaseContext
{
   public class CustomTempTable : ITempTable<int, string>
   {
      public int Column1 { get; set; }
      public string Column2 { get; set; }
   }
}
