namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntityWithShadowProperties
{
   public Guid Id { get; set; }
   public string? Name { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithShadowProperties>(builder =>
                                                          {
                                                             builder.Property<string>("ShadowStringProperty").HasMaxLength(50);
                                                             builder.Property<int?>("ShadowIntProperty");
                                                          });
   }
}
