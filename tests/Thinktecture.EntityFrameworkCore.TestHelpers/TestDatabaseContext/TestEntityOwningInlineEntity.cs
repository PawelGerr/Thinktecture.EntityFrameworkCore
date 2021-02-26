using System;
using System.Collections.Generic;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntityOwningInlineEntity
   {
      public Guid Id { get; set; }

      public OwnedInlineEntity InlineEntity { get; set; }

#nullable disable
      public TestEntityOwningInlineEntity()
      {
      }
#nullable enable
   }
}
