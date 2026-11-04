namespace Thinktecture.TestDatabaseContext;

/// <summary>Not part of the default model; tests register it via <c>ConfigureModel</c>. No table exists; the tests throw before any SQL runs.</summary>
public class TestEntityWithNullableClassComplexType
{
   public Guid Id { get; set; }
   public BoundaryValueObject? OptionalBoundary { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithNullableClassComplexType>(builder => builder.ComplexProperty(e => e.OptionalBoundary, complexBuilder =>
                                                                                                                       {
                                                                                                                          complexBuilder.Property(b => b.Upper);
                                                                                                                          complexBuilder.Property(b => b.Lower);
                                                                                                                       }));
   }
}
