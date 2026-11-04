namespace Thinktecture.TestDatabaseContext;

/// <summary>
/// Not part of the default model; tests register it via <c>ConfigureModel</c> and create the table themselves.
/// </summary>
public class TestEntityWithNestedStructComplexType
{
   public const string TableName = "TestEntitiesWithNestedStructComplexType";

   public Guid Id { get; set; }
   public MessageEnvelope Envelope { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithNestedStructComplexType>(builder =>
                                                                 {
                                                                    builder.ToTable(TableName);
                                                                    builder.Property(e => e.Id).HasColumnName("id");
                                                                    builder.ComplexProperty(e => e.Envelope, envelope =>
                                                                                                             {
                                                                                                                envelope.Property(e => e.Sequence).HasColumnName("sequence");
                                                                                                                envelope.ComplexProperty(e => e.Key, key =>
                                                                                                                                                     {
                                                                                                                                                        key.Property(k => k.ReceivedAt).HasColumnName("received_at");
                                                                                                                                                        key.Property(k => k.MessageTimestamp).HasColumnName("message_timestamp");
                                                                                                                                                     });
                                                                                                             });
                                                                 });
   }
}
