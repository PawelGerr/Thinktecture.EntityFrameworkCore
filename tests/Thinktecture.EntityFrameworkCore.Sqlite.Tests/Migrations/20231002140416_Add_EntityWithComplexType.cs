using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    /// <inheritdoc />
    public partial class Add_EntityWithComplexType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestEntities_with_ComplexType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Boundary_Lower = table.Column<int>(type: "INTEGER", nullable: false),
                    Boundary_Upper = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEntities_with_ComplexType", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestEntities_Id",
                table: "TestEntities",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestEntities_with_ComplexType");

            migrationBuilder.DropIndex(
                name: "IX_TestEntities_Id",
                table: "TestEntities");
        }
    }
}
