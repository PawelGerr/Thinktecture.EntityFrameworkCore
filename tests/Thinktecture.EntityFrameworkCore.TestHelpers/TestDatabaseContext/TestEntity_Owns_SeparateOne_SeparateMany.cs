namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntity_Owns_SeparateOne_SeparateMany
{
   public Guid Id { get; set; }

   public OwnedEntity_Owns_SeparateMany SeparateEntity { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateOne_SeparateMany>(builder => builder.OwnsOne(e => e.SeparateEntity,
                                                                                               navigationBuilder =>
                                                                                               {
                                                                                                  navigationBuilder.ToTable("SeparateEntitiesOne_SeparateMany");
                                                                                                  navigationBuilder.OwnsMany(e => e.SeparateEntities,
                                                                                                                             innerBuilder => innerBuilder.ToTable("SeparateEntitiesOne_SeparateMany_Inner"));
                                                                                               }));
   }
}
