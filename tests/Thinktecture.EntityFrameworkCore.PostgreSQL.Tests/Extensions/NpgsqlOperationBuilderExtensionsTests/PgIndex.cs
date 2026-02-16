namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

public class PgIndex
{
   public required string Indexname { get; set; }
   public required string Indexdef { get; set; }

   private PgIndex()
   {
   }
}
