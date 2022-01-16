namespace Thinktecture.TestDatabaseContext;

public class KeylessTestEntity
{
   public int IntColumn { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<KeylessTestEntity>(builder => builder.HasNoKey());
   }
}
