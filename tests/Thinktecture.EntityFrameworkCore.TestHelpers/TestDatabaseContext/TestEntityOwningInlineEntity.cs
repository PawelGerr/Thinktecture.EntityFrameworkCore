using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
      public static void Configure(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<TestEntityOwningInlineEntity>(builder => builder.OwnsOne(e => e.InlineEntity));
      }
   }
}
