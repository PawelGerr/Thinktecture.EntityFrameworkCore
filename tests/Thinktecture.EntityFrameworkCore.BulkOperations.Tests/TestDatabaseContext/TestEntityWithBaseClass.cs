namespace Thinktecture.TestDatabaseContext;
#pragma warning disable 8618

public class TestEntityWithBaseClass : TestEntityBaseClass
{
   public Guid Id { get; set; }
   public Guid Description { get; set; }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithBaseClass>();
   }
}
