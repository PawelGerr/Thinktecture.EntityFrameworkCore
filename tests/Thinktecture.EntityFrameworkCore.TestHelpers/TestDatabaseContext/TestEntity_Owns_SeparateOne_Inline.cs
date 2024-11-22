namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618
public class TestEntity_Owns_SeparateOne_Inline
{
   public Guid Id { get; set; }

   public OwnedEntity_Owns_Inline SeparateEntity { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_SeparateOne_Inline>(builder => builder.OwnsOne(e => e.SeparateEntity,
                                                                                         navigationBuilder =>
                                                                                         {
                                                                                            navigationBuilder.ToTable("SeparateEntitiesOne_Inline");
                                                                                            navigationBuilder.OwnsOne(e => e.InlineEntity);
                                                                                         }));
   }
}
