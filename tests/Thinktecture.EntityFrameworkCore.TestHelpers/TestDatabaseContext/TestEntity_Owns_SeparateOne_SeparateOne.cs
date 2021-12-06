using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

// ReSharper disable InconsistentNaming
namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntity_Owns_SeparateOne_SeparateOne
{
   public Guid Id { get; set; }

   public OwnedEntity_Owns_SeparateOne SeparateEntity { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateOne_SeparateOne>(builder => builder.OwnsOne(e => e.SeparateEntity,
                                                                                              navigationBuilder =>
                                                                                              {
                                                                                                 navigationBuilder.ToTable("SeparateEntitiesOne_SeparateOne");
                                                                                                 navigationBuilder.OwnsOne(e => e.SeparateEntity,
                                                                                                                           innerBuilder => innerBuilder.ToTable("SeparateEntitiesOne_SeparateOne_Inner"));
                                                                                              }));
   }
}