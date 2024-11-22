using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once UnusedMember.Global
   // ReSharper disable once InconsistentNaming
   public partial class Initial_Migration : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntities",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  Name = table.Column<string>(nullable: true),
                                                  Count = table.Column<int>()
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntities");
      }
   }
}
