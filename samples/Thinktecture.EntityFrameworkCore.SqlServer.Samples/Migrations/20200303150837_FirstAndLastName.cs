using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class FirstAndLastName : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<string>("FirstName", "Customers", maxLength: 100, nullable: false, defaultValue: "First");
         migrationBuilder.AddColumn<string>("LastName", "Customers", maxLength: 100, nullable: false, defaultValue: "Last");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("FirstName", "Customers");
         migrationBuilder.DropColumn("LastName", "Customers");
      }
   }
}
