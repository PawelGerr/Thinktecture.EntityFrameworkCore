using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
      public static void Configure(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<TestEntityOwningManyEntities>(builder => builder.OwnsMany(e => e.SeparateEntities,
                                                                                       navigationBuilder => navigationBuilder.ToTable("SeparateEntities_Many")));
      }
   }
}
