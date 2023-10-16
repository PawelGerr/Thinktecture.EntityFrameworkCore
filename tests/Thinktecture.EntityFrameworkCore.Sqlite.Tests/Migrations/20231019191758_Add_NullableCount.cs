using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    /// <inheritdoc />
    public partial class Add_NullableCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NullableCount",
                table: "TestEntities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestEntities_Id",
                table: "TestEntities",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestEntities_Id",
                table: "TestEntities");

            migrationBuilder.DropColumn(
                name: "NullableCount",
                table: "TestEntities");
        }
    }
}
