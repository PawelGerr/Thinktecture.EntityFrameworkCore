namespace Thinktecture.TestDatabaseContext;

public class TestEntityWithRowVersion
{
   public Guid Id { get; set; }
   public string? Name { get; set; }
   public long RowVersion { get; set; }
}
