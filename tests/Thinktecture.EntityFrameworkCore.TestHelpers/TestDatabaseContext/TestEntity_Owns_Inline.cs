using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

public class TestEntity_Owns_Inline
{
   public Guid Id { get; set; }

   public OwnedEntity InlineEntity { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_Inline>(builder => builder.OwnsOne(e => e.InlineEntity));
   }
}