using System;
using Microsoft.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntityWithSqlDefaultValues
{
   public Guid Id { get; set; }
   public int Int { get; set; }
   public int? NullableInt { get; set; }
   public string String { get; set; }
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