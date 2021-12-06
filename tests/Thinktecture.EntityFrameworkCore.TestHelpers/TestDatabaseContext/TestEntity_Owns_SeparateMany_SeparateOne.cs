

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

public class TestEntity_Owns_SeparateMany_SeparateOne
{
   public Guid Id { get; set; }

   public List<OwnedEntity_Owns_SeparateOne> SeparateEntities { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateMany_SeparateOne>(builder => builder.OwnsMany(e => e.SeparateEntities,
                                                                                                navigationBuilder =>
                                                                                                {
                                                                                                   navigationBuilder.ToTable("SeparateEntitiesMany_SeparateEntitiesOne");
                                                                                                   navigationBuilder.OwnsOne(e => e.SeparateEntity,
                                                                                                                             innerBuilder => innerBuilder.ToTable("SeparateEntitiesMany_SeparateEntitiesOne_Inner"));
                                                                                                }));
   }
}
