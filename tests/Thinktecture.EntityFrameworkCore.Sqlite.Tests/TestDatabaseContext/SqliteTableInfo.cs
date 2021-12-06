namespace Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
public class SqliteTableInfo
{
   public long CId { get; set; }
   public string? Name { get; set; }
   public string? Type { get; set; }
   public long? NotNull { get; set; }
   public byte[]? Dflt_Value { get; set; }
   public long? PK { get; set; }
}