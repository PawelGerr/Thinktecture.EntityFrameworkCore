using System;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class TestEntityWithSqlDefaultValues
   {
      public Guid Id { get; set; }
      public int Int { get; set; }
      public int? NullableInt { get; set; }
#pragma warning disable 8618
      public string String { get; set; }
#pragma warning restore 8618
      public string? NullableString { get; set; }

      public static void Configure(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<TestEntityWithSqlDefaultValues>(builder =>
                                                             {
                                                                builder.Property(e => e.Int).HasDefaultValueSql("1");
                                                                builder.Property(e => e.NullableInt).HasDefaultValueSql("2");
                                                                builder.Property(e => e.String).HasDefaultValueSql("'3'");
                                                                builder.Property(e => e.NullableString).HasDefaultValueSql("'4'");
                                                             });
      }
   }
}
