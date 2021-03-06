using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext
{
   public class TestEntity_Owns_SeparateMany_SeparateMany
   {
      public Guid Id { get; set; }

      public List<OwnedEntity_Owns_SeparateMany> SeparateEntities { get; set; }

      public static void Configure(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<TestEntity_Owns_SeparateMany_SeparateMany>(builder => builder.OwnsMany(e => e.SeparateEntities,
                                                                                                    navigationBuilder =>
                                                                                                    {
                                                                                                       navigationBuilder.ToTable("SeparateEntitiesMany_SeparateEntitiesMany");
                                                                                                       navigationBuilder.OwnsMany(e => e.SeparateEntities,
                                                                                                                                  innerBuilder => innerBuilder.ToTable("SeparateEntitiesMany_SeparateEntitiesMany_Inner"));
                                                                                                    }));
      }
   }
}
