namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithDifferentColumnNames
{
   public Guid Id { get; set; }
   public string? Name { get; set; }
   public int Count { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithDifferentColumnNames>(builder =>
                                                              {
                                                                 builder.Property(e => e.Id).HasColumnName("entity_id");
                                                                 builder.Property(e => e.Name).HasColumnName("display_name");
                                                                 builder.Property(e => e.Count).HasColumnName("item_count");
                                                              });
   }
}
