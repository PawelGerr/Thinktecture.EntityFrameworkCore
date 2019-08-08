using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Add_Entity_with_AutoIncrement : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntitiesWithAutoIncrement",
                                      table => new
                                               {
                                                  Id = table.Column<int>()
                                                            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                                                  Name = table.Column<string>(nullable: true)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesWithAutoIncrement", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntitiesWithAutoIncrement");
      }
   }
}
