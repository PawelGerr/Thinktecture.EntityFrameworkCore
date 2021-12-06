

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

public class TestEntity_Owns_SeparateMany_Inline
{
   public Guid Id { get; set; }

   public List<OwnedEntity_Owns_Inline> SeparateEntities { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateMany_Inline>(builder => builder.OwnsMany(e => e.SeparateEntities,
                                                                                           navigationBuilder =>
                                                                                           {
                                                                                              navigationBuilder.ToTable("SeparateEntitiesMany_Inline");
                                                                                              navigationBuilder.OwnsOne(e => e.InlineEntity);
                                                                                           }));
   }
}
