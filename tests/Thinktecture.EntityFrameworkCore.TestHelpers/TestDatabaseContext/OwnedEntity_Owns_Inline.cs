// ReSharper disable InconsistentNaming
namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class OwnedEntity_Owns_Inline
{
   public string? StringColumn { get; set; }
   public int IntColumn { get; set; }

   public OwnedEntity InlineEntity { get; set; }
}