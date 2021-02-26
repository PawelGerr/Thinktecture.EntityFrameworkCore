using System;
using System.Collections.Generic;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntityOwningOneSeparateEntity
   {
      public Guid Id { get; set; }

      public OwnedSeparateEntity SeparateEntity { get; set; }

#nullable disable
      public TestEntityOwningOneSeparateEntity()
      {
      }
#nullable enable
   }
}
