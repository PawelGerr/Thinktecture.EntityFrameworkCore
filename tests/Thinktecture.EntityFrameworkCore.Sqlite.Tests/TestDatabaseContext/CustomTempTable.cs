namespace Thinktecture.TestDatabaseContext
{
   public class CustomTempTable
   {
      public int Column1 { get; set; }
      public string Column2 { get; set; }

#nullable disable
      public CustomTempTable()
      {
      }
#nullable enable

      public CustomTempTable(int column1, string column2)
      {
         Column1 = column1;
         Column2 = column2;
      }
   }
}
