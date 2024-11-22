

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

public class TestEntity_Owns_SeparateMany
{
   public Guid Id { get; set; }

   public List<OwnedEntity> SeparateEntities { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateMany>(builder => builder.OwnsMany(e => e.SeparateEntities,
                                                                                    navigationBuilder => navigationBuilder.ToTable("SeparateEntitiesMany")));
   }
}
