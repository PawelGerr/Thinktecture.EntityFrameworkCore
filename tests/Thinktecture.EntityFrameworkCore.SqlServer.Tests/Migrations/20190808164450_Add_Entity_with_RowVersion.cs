using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   public partial class Add_Entity_with_RowVersion : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntitiesWithRowVersion",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  Name = table.Column<string>(nullable: true),
                                                  RowVersion = table.Column<byte[]>(rowVersion: true)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesWithRowVersion", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntitiesWithRowVersion");
      }
   }
}
