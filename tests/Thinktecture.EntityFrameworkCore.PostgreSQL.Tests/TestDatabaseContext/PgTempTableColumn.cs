namespace Thinktecture.TestDatabaseContext;

public class PgTempTableColumn
{
   public string? ColumnName { get; set; }
   public string? DataType { get; set; }
   public bool IsNullable { get; set; }
   public string? ColumnDefault { get; set; }
   public string? CollationName { get; set; }
}
