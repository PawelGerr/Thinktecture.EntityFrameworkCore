

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

public class TestEntity_Owns_SeparateOne
{
   public Guid Id { get; set; }

   public OwnedEntity? SeparateEntity { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateOne>(builder => builder.OwnsOne(e => e.SeparateEntity,
                                                                                  navigationBuilder => navigationBuilder.ToTable("SeparateEntitiesOne")));
   }
}
