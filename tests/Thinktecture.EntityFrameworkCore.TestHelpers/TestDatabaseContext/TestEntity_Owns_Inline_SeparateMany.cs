

// ReSharper disable InconsistentNaming
#pragma warning disable 8618
namespace Thinktecture.TestDatabaseContext;

public class TestEntity_Owns_Inline_SeparateMany
{
   public Guid Id { get; set; }

   public OwnedEntity_Owns_SeparateMany InlineEntity { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntity_Owns_Inline_SeparateMany>(builder =>
                                                               {
                                                                  builder.Property(e => e.Id).ValueGeneratedNever();
                                                                  builder.OwnsOne(e => e.InlineEntity,
                                                                                  navigationBuilder => navigationBuilder.OwnsMany(e => e.SeparateEntities,
                                                                                                                                  innerBuilder => innerBuilder.ToTable("InlineEntities_SeparateMany")));
                                                               });
   }
}
