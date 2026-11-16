namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithStructComplexType
{
   public Guid Id { get; set; }
   public long MessageTimestamp { get; set; }
   public TimestampPair EffectiveMessage { get; set; }
   public string? Name { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithStructComplexType>(builder =>
                                                           {
                                                              builder.HasKey(e => new { e.Id, e.MessageTimestamp });
                                                              builder.Property(e => e.Id).HasColumnName("id");
                                                              builder.Property(e => e.MessageTimestamp).HasColumnName("message_timestamp");
                                                              builder.ComplexProperty(e => e.EffectiveMessage, c =>
                                                                                                               {
                                                                                                                  c.Property(k => k.ReceivedAt).HasColumnName("effective_received_at");
                                                                                                                  c.Property(k => k.MessageTimestamp).HasColumnName("effective_message_timestamp");
                                                                                                               });
                                                           });
   }
}
