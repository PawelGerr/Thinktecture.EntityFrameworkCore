using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

public class DelegatingMigration : Migration
{
   public Action<MigrationBuilder>? ConfigureUp { get; set; }

   protected override void Up(MigrationBuilder migrationBuilder)
   {
      ConfigureUp?.Invoke(migrationBuilder);
   }
}
