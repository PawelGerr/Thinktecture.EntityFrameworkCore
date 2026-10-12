// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

// Two owned navigations with the same CLR type and separate tables. The root has a non-key column so that an UPDATE of the root is valid.
public class TestEntity_Owns_TwoSeparateOnes
{
   public Guid Id { get; set; }
   public string? Name { get; set; }
   public OwnedEntity First { get; set; }
   public OwnedEntity Second { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_TwoSeparateOnes>(builder =>
                                                            {
                                                               builder.ToTable("TestEntities_Own_TwoSeparateOnes");
                                                               builder.OwnsOne(e => e.First, navigationBuilder => navigationBuilder.ToTable("TwoSeparateOnes_First"));
                                                               builder.OwnsOne(e => e.Second, navigationBuilder => navigationBuilder.ToTable("TwoSeparateOnes_Second"));
                                                            });
   }
}
