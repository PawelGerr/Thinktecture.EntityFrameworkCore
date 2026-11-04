namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithComplexCollection
{
   public Guid Id { get; set; }
   public List<BoundaryValueObject> Boundaries { get; set; } = [];

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithComplexCollection>(builder => builder.ComplexCollection(e => e.Boundaries, c =>
                                                                                                                {
                                                                                                                   c.ToJson();
                                                                                                                   c.Property(b => b.Upper);
                                                                                                                   c.Property(b => b.Lower);
                                                                                                                }));
   }
}
