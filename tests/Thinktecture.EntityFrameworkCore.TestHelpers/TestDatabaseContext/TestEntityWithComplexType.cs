namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithComplexType
{
   public Guid Id { get; }

   private BoundaryValueObject? _boundary;
   public BoundaryValueObject Boundary
   {
      get => _boundary ?? throw new Exception("Complex Type not set.");
      set => _boundary = value;
   }

   // EF
   private TestEntityWithComplexType(Guid id)
   {
      Id = id;
   }

   public TestEntityWithComplexType(Guid id, BoundaryValueObject boundary)
   {
      Id = id;
      _boundary = boundary;
   }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithComplexType>(builder =>
                                                     {
                                                        builder.Property(e => e.Id);
                                                        builder.ComplexProperty(e => e.Boundary,
                                                                                builder =>
                                                                                {
                                                                                   builder.Property(b => b.Upper);
                                                                                   builder.Property(b => b.Lower);
                                                                                });
                                                     });
   }
}
