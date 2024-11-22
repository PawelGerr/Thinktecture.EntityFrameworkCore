// ReSharper disable InconsistentNaming
namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class OwnedEntity_Owns_SeparateOne
{
   public string? StringColumn { get; set; }
   public int IntColumn { get; set; }

   public OwnedEntity SeparateEntity { get; set; }
}