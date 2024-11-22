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
                name: "TestEntitiesWithBaseClass",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEntitiesWithBaseClass", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestEntityWithCollation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnWithoutCollation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnWithCollation = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Japanese_CI_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEntityWithCollation", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestEntitiesWithBaseClass");

            migrationBuilder.DropTable(
                name: "TestEntityWithCollation");
        }
    }
}
