using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once UnusedMember.Global
   // ReSharper disable once InconsistentNaming
   public partial class Add_shadow_and_private_properties : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<int>("PropertyWithBackingField", "TestEntities");
         migrationBuilder.AddColumn<string>("ShadowProperty", "TestEntities", maxLength: 50, nullable: true);
         migrationBuilder.AddColumn<int>("_privateField", "TestEntities");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("PropertyWithBackingField", "TestEntities");
         migrationBuilder.DropColumn("ShadowProperty", "TestEntities");
         migrationBuilder.DropColumn("_privateField", "TestEntities");
      }
   }
}
