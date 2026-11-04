using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thinktecture.TestDatabaseContext;

public readonly record struct ConvertedTimestampPair(TimestampValue ReceivedAt, TimestampValue MessageTimestamp)
{
   public static void Configure(ComplexPropertyBuilder<ConvertedTimestampPair> builder)
   {
      builder.Property(k => k.ReceivedAt).HasColumnName("effective_received_at").HasConversion(v => v.Value, v => new TimestampValue(v));
      builder.Property(k => k.MessageTimestamp).HasColumnName("effective_message_timestamp").HasConversion(v => v.Value, v => new TimestampValue(v));
   }
}
