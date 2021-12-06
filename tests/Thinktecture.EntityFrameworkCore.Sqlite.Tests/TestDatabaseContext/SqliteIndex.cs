namespace Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
public class SqliteIndex
{
   public byte[] Seq { get; set; }
   public byte[] Name { get; set; }
   public byte[] Unique { get; set; }
   public byte[] Origin { get; set; }
   public byte[] Partial { get; set; }
}