namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithFlags
{
   public Guid Id { get; set; }
   public int GroupId { get; set; }
   public PhaseMembership Phase { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithFlags>();
   }
}
