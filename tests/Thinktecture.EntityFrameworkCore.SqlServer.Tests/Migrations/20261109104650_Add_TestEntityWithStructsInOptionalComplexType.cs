using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thinktecture.Migrations
{
    /// <inheritdoc />
    public partial class Add_TestEntityWithStructsInOptionalComplexType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestEntitiesWithStructsInOptionalComplexType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionalOuter_Y = table.Column<int>(type: "int", nullable: true),
                    OptionalOuter_OptionalPair_MessageTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    OptionalOuter_OptionalPair_ReceivedAt = table.Column<long>(type: "bigint", nullable: true),
                    OptionalOuter_Pair_MessageTimestamp = table.Column<long>(type: "bigint", nullable: true),
                    OptionalOuter_Pair_ReceivedAt = table.Column<long>(type: "bigint", nullable: true),
                    OptionalOuter_TaggedStruct_Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionalOuter_TaggedStruct_X = table.Column<int>(type: "int", nullable: true),
                    RequiredPair_MessageTimestamp = table.Column<long>(type: "bigint", nullable: false),
                    RequiredPair_ReceivedAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestEntitiesWithStructsInOptionalComplexType", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestEntitiesWithStructsInOptionalComplexType");
        }
    }
}
