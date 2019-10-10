using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   public partial class Add_ConvertibleClass : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<int>("ConvertibleClass", "TestEntities", nullable: true);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("ConvertibleClass", "TestEntities");
      }
   }
}
