using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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

      public static void Configure(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<TestEntityOwningOneSeparateEntity>(builder => builder.OwnsOne(e => e.SeparateEntity,
                                                                                           navigationBuilder => navigationBuilder.ToTable("SeparateEntities_One")));
      }
   }
}
