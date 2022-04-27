namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithCollation
{
   public Guid Id { get; set; }
   public string ColumnWithoutCollation { get; set; }
   public string ColumnWithCollation { get; set; }

   public TestEntityWithCollation(Guid id, string columnWithoutCollation, string columnWithCollation)
   {
      Id = id;
      ColumnWithoutCollation = columnWithoutCollation;
      ColumnWithCollation = columnWithCollation;
   }

   public static void Configure(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<TestEntityWithCollation>(builder => builder.Property(e => e.ColumnWithCollation).UseCollation("Japanese_CI_AS"));
   }
}
