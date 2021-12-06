using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   public partial class Add_Entity_with_ShadowProperties : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("ShadowProperty", "TestEntities");

         migrationBuilder.CreateTable("TestEntitiesWithShadowProperties",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  Name = table.Column<string>(nullable: true),
                                                  ShadowIntProperty = table.Column<int>(nullable: true),
                                                  ShadowStringProperty = table.Column<string>(maxLength: 50, nullable: true)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesWithShadowProperties", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntitiesWithShadowProperties");

         migrationBuilder.AddColumn<string>("ShadowProperty", "TestEntities", maxLength: 50, nullable: true);
      }
   }
}
