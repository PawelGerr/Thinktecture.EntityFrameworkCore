using System.Collections.Generic;

namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class OwnedEntity_Owns_SeparateMany
{
   public string? StringColumn { get; set; }
   public int IntColumn { get; set; }

   public List<OwnedEntity> SeparateEntities { get; set; }
}