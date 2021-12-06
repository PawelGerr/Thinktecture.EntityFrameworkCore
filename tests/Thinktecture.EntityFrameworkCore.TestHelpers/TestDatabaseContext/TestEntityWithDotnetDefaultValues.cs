namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntityWithDotnetDefaultValues
{
   public Guid Id { get; set; }
   public int Int { get; set; }
   public int? NullableInt { get; set; }
   public string String { get; set; }
   public string? NullableString { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithDotnetDefaultValues>(builder =>
                                                             {
                                                                builder.Property(e => e.Id).HasDefaultValue(new Guid("0B151271-79BB-4F6C-B85F-E8F61300FF1B"));
                                                                builder.Property(e => e.Int).HasDefaultValue(1);
                                                                builder.Property(e => e.NullableInt).HasDefaultValue(2);
                                                                builder.Property(e => e.String).HasDefaultValue("3");
                                                                builder.Property(e => e.NullableString).HasDefaultValue("4");
                                                             });
   }
}
