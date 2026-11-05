namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithNullableComplexTypes
{
   public Guid Id { get; set; }
   public BoundaryValueObject? OptionalBoundary { get; set; }
   public TimestampPair? OptionalPair { get; set; }
   public TaggedValues? Tagged { get; set; }
   public OuterWithOptionalInner Outer { get; set; } = new();
   public OptionalOuter? OptionalOuter { get; set; }
   public ConvertedTimestampPair? OptionalConverted { get; set; }

   public static TestEntityWithNullableComplexTypes CreateWithValues(Guid id) => new()
                                                                                 {
                                                                                    Id = id,
                                                                                    OptionalBoundary = new BoundaryValueObject(2, 5),
                                                                                    OptionalPair = new TimestampPair(100, 200),
                                                                                    Tagged = new TaggedValues { X = 7, Text = "t" },
                                                                                    Outer = new OuterWithOptionalInner { Z = 1, Inner = new InnerValue { V = 11 } },
                                                                                    OptionalOuter = new OptionalOuter { Y = 3, Inner = new InnerValue { V = 33 } },
                                                                                    OptionalConverted = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2))
                                                                                 };

   public static TestEntityWithNullableComplexTypes CreateWithNulls(Guid id) => new() { Id = id, Outer = new OuterWithOptionalInner { Z = 1, Inner = null } };

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithNullableComplexTypes>(builder =>
                                                              {
                                                                 builder.ToTable("TestEntitiesWithNullableComplexTypes");
                                                                 builder.ComplexProperty(e => e.OptionalBoundary, b =>
                                                                                                                  {
                                                                                                                     b.Property(x => x.Upper);
                                                                                                                     b.Property(x => x.Lower);
                                                                                                                  });
                                                                 builder.ComplexProperty(e => e.OptionalPair);
                                                                 builder.ComplexProperty(e => e.Tagged, b => b.HasDiscriminator());
                                                                 builder.ComplexProperty(e => e.Outer, b => b.ComplexProperty(o => o.Inner));
                                                                 builder.ComplexProperty(e => e.OptionalOuter, b => b.ComplexProperty(o => o.Inner));
                                                                 builder.ComplexProperty(e => e.OptionalConverted, b =>
                                                                                                                   {
                                                                                                                      b.Property(p => p.ReceivedAt).HasConversion(v => v.Value, v => new TimestampValue(v));
                                                                                                                      b.Property(p => p.MessageTimestamp).HasConversion(v => v.Value, v => new TimestampValue(v));
                                                                                                                   });
                                                              });
   }
}
