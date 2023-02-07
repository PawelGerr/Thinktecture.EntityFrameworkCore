using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    public partial class Add_TestEntityWithBaseClass : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeylessEntities",
                columns: table => new
                {
                    IntColumn = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "TestEntitiesWithBaseClass",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEntitiesWithBaseClass", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeylessEntities");

            migrationBuilder.DropTable(
                name: "TestEntitiesWithBaseClass");
        }
    }
}
