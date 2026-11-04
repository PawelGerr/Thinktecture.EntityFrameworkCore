namespace Thinktecture.TestDatabaseContext;

/// <summary>Not part of the default model; tests register it via <c>ConfigureModel</c>. No table exists; the tests throw before any SQL runs.</summary>
public class TestEntityWithNullableStructComplexType
{
   public Guid Id { get; set; }
   public TimestampPair? EffectiveMessage { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithNullableStructComplexType>(builder => builder.ComplexProperty(e => e.EffectiveMessage));
   }
}
