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
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NullableCount",
                table: "TestEntities");
        }
    }
}
