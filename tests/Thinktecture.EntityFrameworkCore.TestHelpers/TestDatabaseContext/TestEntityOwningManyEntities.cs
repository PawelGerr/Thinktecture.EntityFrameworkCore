using System;
using System.Collections.Generic;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntityOwningManyEntities
   {
      public Guid Id { get; set; }

      public List<OwnedSeparateEntity> SeparateEntities { get; set; }

#nullable disable
      public TestEntityOwningManyEntities()
      {
      }
#nullable enable
   }
}
