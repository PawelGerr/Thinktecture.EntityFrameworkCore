namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

public class PgIdentityColumn
{
   public string Column_Name { get; set; } = null!;
   public string Is_Identity { get; set; } = null!;

#nullable disable
   private PgIdentityColumn()
   {
   }
#nullable enable
}
