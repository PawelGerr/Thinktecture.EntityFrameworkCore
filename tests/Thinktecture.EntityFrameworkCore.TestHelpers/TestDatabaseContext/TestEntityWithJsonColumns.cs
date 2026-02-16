namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithJsonColumns
{
   public Guid Id { get; set; }
   public string? JsonbColumn { get; set; }
   public string? JsonColumn { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithJsonColumns>(builder =>
                                                     {
                                                        builder.Property(e => e.JsonbColumn).HasColumnType("jsonb");
                                                        builder.Property(e => e.JsonColumn).HasColumnType("json");
                                                     });
   }
}
