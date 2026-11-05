using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    /// <inheritdoc />
    public partial class Add_TestEntityWithNullableComplexTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestEntitiesWithNullableComplexTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionalBoundary_Lower = table.Column<int>(type: "int", nullable: true),
                    OptionalBoundary_Upper = table.Column<int>(type: "int", nullable: true),
                    OptionalConverted_MessageTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    OptionalConverted_ReceivedAt = table.Column<long>(type: "bigint", nullable: true),
                    OptionalOuter_Y = table.Column<int>(type: "int", nullable: true),
                    OptionalOuter_Inner_V = table.Column<int>(type: "int", nullable: true),
                    OptionalPair_MessageTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    OptionalPair_ReceivedAt = table.Column<long>(type: "bigint", nullable: true),
                    Outer_Z = table.Column<int>(type: "int", nullable: false),
                    Outer_Inner_V = table.Column<int>(type: "int", nullable: true),
                    Tagged_Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tagged_Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tagged_X = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEntitiesWithNullableComplexTypes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestEntitiesWithNullableComplexTypes");
        }
    }
}
