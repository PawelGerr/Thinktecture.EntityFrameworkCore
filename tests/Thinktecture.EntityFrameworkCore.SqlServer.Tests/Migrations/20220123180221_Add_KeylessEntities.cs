using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    public partial class Add_KeylessEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable("KeylessEntities",
                                         table => new
                                                           {
                                                              IntColumn = table.Column<int>(type: "int", nullable: false)
                                                           });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("KeylessEntities");
        }
    }
}
