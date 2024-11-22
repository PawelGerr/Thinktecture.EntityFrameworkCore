using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   // ReSharper disable once UnusedMember.Global
   public partial class Initial_Migration : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntities",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  Name = table.Column<string>(nullable: true),
                                                  Count = table.Column<int>(),
                                                  PropertyWithBackingField = table.Column<int>(),
                                                  _privateField = table.Column<int>()
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities", x => x.Id));

         migrationBuilder.CreateTable("TestEntitiesWithAutoIncrement",
                                      table => new
                                               {
                                                  Id = table.Column<int>()
                                                            .Annotation("Sqlite:Autoincrement", true),
                                                  Name = table.Column<string>(nullable: true)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesWithAutoIncrement", x => x.Id));

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
         migrationBuilder.DropTable("TestEntities");
         migrationBuilder.DropTable("TestEntitiesWithAutoIncrement");
         migrationBuilder.DropTable("TestEntitiesWithShadowProperties");
      }
   }
}
