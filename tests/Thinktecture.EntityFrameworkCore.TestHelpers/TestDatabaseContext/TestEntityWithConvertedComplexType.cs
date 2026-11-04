namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithConvertedComplexType
{
   public Guid Id { get; set; }
   public long MessageTimestamp { get; set; }
   public ConvertedTimestampPair EffectiveMessage { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithConvertedComplexType>(builder =>
                                                              {
                                                                 builder.HasKey(e => new { e.Id, e.MessageTimestamp });
                                                                 builder.Property(e => e.Id).HasColumnName("id");
                                                                 builder.Property(e => e.MessageTimestamp).HasColumnName("message_timestamp");
                                                                 builder.ComplexProperty(e => e.EffectiveMessage, ConvertedTimestampPair.Configure);
                                                              });
   }
}
