using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   // ReSharper disable once UnusedMember.Global
   public partial class Property_with_Converter : Migration
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
