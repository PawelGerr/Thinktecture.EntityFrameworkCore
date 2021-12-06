using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Add_tables_with_owned_entities : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntities_Own_Inline",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_Inline", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_Inline_Inline",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false),
                                                  InlineEntity_InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_Inline_Inline", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_Inline_SeparateMany",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_Inline_SeparateMany", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_Inline_SeparateOne",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_Inline_SeparateOne", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateMany",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateMany", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateMany_Inline",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateMany_Inline", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateMany_SeparateMany",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateMany_SeparateMany", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateMany_SeparateOne",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateMany_SeparateOne", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateOne",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateOne", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateOne_Inline",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateOne_Inline", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateOne_SeparateMany",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateOne_SeparateMany", x => x.Id));

         migrationBuilder.CreateTable("TestEntities_Own_SeparateOne_SeparateOne",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities_Own_SeparateOne_SeparateOne", x => x.Id));

         migrationBuilder.CreateTable("InlineEntities_SeparateMany",
                                      table => new
                                               {
                                                  OwnedEntity_Owns_SeparateManyTestEntity_Owns_Inline_SeparateManyId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_InlineEntities_SeparateMany", x => new { x.OwnedEntity_Owns_SeparateManyTestEntity_Owns_Inline_SeparateManyId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_InlineEntities_SeparateMany_TestEntities_Own_Inline_SeparateMany_OwnedEntity_Owns_SeparateManyTestEntity_Owns_Inline_Separat~",
                                                                       x => x.OwnedEntity_Owns_SeparateManyTestEntity_Owns_Inline_SeparateManyId,
                                                                       "TestEntities_Own_Inline_SeparateMany",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("InlineEntities_SeparateOne",
                                      table => new
                                               {
                                                  OwnedEntity_Owns_SeparateOneTestEntity_Owns_Inline_SeparateOneId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_InlineEntities_SeparateOne", x => x.OwnedEntity_Owns_SeparateOneTestEntity_Owns_Inline_SeparateOneId);
                                                      table.ForeignKey(
                                                                       "FK_InlineEntities_SeparateOne_TestEntities_Own_Inline_SeparateOne_OwnedEntity_Owns_SeparateOneTestEntity_Owns_Inline_SeparateOn~",
                                                                       x => x.OwnedEntity_Owns_SeparateOneTestEntity_Owns_Inline_SeparateOneId,
                                                                       "TestEntities_Own_Inline_SeparateOne",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesMany",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateManyId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesMany", x => new { x.TestEntity_Owns_SeparateManyId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesMany_TestEntities_Own_SeparateMany_TestEntity_Owns_SeparateManyId",
                                                                       x => x.TestEntity_Owns_SeparateManyId,
                                                                       "TestEntities_Own_SeparateMany",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesMany_Inline",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateMany_InlineId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesMany_Inline", x => new { x.TestEntity_Owns_SeparateMany_InlineId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesMany_Inline_TestEntities_Own_SeparateMany_Inline_TestEntity_Owns_SeparateMany_InlineId",
                                                                       x => x.TestEntity_Owns_SeparateMany_InlineId,
                                                                       "TestEntities_Own_SeparateMany_Inline",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesMany_SeparateEntitiesMany",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateMany_SeparateManyId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesMany_SeparateEntitiesMany", x => new { x.TestEntity_Owns_SeparateMany_SeparateManyId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesMany_SeparateEntitiesMany_TestEntities_Own_SeparateMany_SeparateMany_TestEntity_Owns_SeparateMany_SeparateMa~",
                                                                       x => x.TestEntity_Owns_SeparateMany_SeparateManyId,
                                                                       "TestEntities_Own_SeparateMany_SeparateMany",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesMany_SeparateEntitiesOne",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateMany_SeparateOneId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesMany_SeparateEntitiesOne", x => new { x.TestEntity_Owns_SeparateMany_SeparateOneId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesMany_SeparateEntitiesOne_TestEntities_Own_SeparateMany_SeparateOne_TestEntity_Owns_SeparateMany_SeparateOneId",
                                                                       x => x.TestEntity_Owns_SeparateMany_SeparateOneId,
                                                                       "TestEntities_Own_SeparateMany_SeparateOne",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesOne",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateOneId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesOne", x => x.TestEntity_Owns_SeparateOneId);
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesOne_TestEntities_Own_SeparateOne_TestEntity_Owns_SeparateOneId",
                                                                       x => x.TestEntity_Owns_SeparateOneId,
                                                                       "TestEntities_Own_SeparateOne",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesOne_Inline",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateOne_InlineId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesOne_Inline", x => x.TestEntity_Owns_SeparateOne_InlineId);
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesOne_Inline_TestEntities_Own_SeparateOne_Inline_TestEntity_Owns_SeparateOne_InlineId",
                                                                       x => x.TestEntity_Owns_SeparateOne_InlineId,
                                                                       "TestEntities_Own_SeparateOne_Inline",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesOne_SeparateMany",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateOne_SeparateManyId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesOne_SeparateMany", x => x.TestEntity_Owns_SeparateOne_SeparateManyId);
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesOne_SeparateMany_TestEntities_Own_SeparateOne_SeparateMany_TestEntity_Owns_SeparateOne_SeparateManyId",
                                                                       x => x.TestEntity_Owns_SeparateOne_SeparateManyId,
                                                                       "TestEntities_Own_SeparateOne_SeparateMany",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesOne_SeparateOne",
                                      table => new
                                               {
                                                  TestEntity_Owns_SeparateOne_SeparateOneId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesOne_SeparateOne", x => x.TestEntity_Owns_SeparateOne_SeparateOneId);
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesOne_SeparateOne_TestEntities_Own_SeparateOne_SeparateOne_TestEntity_Owns_SeparateOne_SeparateOneId",
                                                                       x => x.TestEntity_Owns_SeparateOne_SeparateOneId,
                                                                       "TestEntities_Own_SeparateOne_SeparateOne",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesMany_SeparateEntitiesMany_Inner",
                                      table => new
                                               {
                                                  OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  OwnedEntity_Owns_SeparateManyId = table.Column<int>("int", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesMany_SeparateEntitiesMany_Inner", x => new { x.OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId, x.OwnedEntity_Owns_SeparateManyId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesMany_SeparateEntitiesMany_Inner_SeparateEntitiesMany_SeparateEntitiesMany_OwnedEntity_Owns_SeparateManyTestE~",
                                                                       x => new { x.OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId, x.OwnedEntity_Owns_SeparateManyId },
                                                                       "SeparateEntitiesMany_SeparateEntitiesMany",
                                                                       new[] { "TestEntity_Owns_SeparateMany_SeparateManyId", "Id" },
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesMany_SeparateEntitiesOne_Inner",
                                      table => new
                                               {
                                                  OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateMany_SeparateOneId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  OwnedEntity_Owns_SeparateOneId = table.Column<int>("int", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesMany_SeparateEntitiesOne_Inner", x => new { x.OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateMany_SeparateOneId, x.OwnedEntity_Owns_SeparateOneId });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesMany_SeparateEntitiesOne_Inner_SeparateEntitiesMany_SeparateEntitiesOne_OwnedEntity_Owns_SeparateOneTestEnti~",
                                                                       x => new { x.OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateMany_SeparateOneId, x.OwnedEntity_Owns_SeparateOneId },
                                                                       "SeparateEntitiesMany_SeparateEntitiesOne",
                                                                       new[] { "TestEntity_Owns_SeparateMany_SeparateOneId", "Id" },
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesOne_SeparateMany_Inner",
                                      table => new
                                               {
                                                  OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateOne_SeparateManyId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesOne_SeparateMany_Inner", x => new { x.OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateOne_SeparateManyId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesOne_SeparateMany_Inner_SeparateEntitiesOne_SeparateMany_OwnedEntity_Owns_SeparateManyTestEntity_Owns_Separat~",
                                                                       x => x.OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateOne_SeparateManyId,
                                                                       "SeparateEntitiesOne_SeparateMany",
                                                                       "TestEntity_Owns_SeparateOne_SeparateManyId",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntitiesOne_SeparateOne_Inner",
                                      table => new
                                               {
                                                  OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateOne_SeparateOneId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntitiesOne_SeparateOne_Inner", x => x.OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateOne_SeparateOneId);
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntitiesOne_SeparateOne_Inner_SeparateEntitiesOne_SeparateOne_OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateOn~",
                                                                       x => x.OwnedEntity_Owns_SeparateOneTestEntity_Owns_SeparateOne_SeparateOneId,
                                                                       "SeparateEntitiesOne_SeparateOne",
                                                                       "TestEntity_Owns_SeparateOne_SeparateOneId",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("InlineEntities_SeparateMany");
         migrationBuilder.DropTable("InlineEntities_SeparateOne");
         migrationBuilder.DropTable("SeparateEntitiesMany");
         migrationBuilder.DropTable("SeparateEntitiesMany_Inline");
         migrationBuilder.DropTable("SeparateEntitiesMany_SeparateEntitiesMany_Inner");
         migrationBuilder.DropTable("SeparateEntitiesMany_SeparateEntitiesOne_Inner");
         migrationBuilder.DropTable("SeparateEntitiesOne");
         migrationBuilder.DropTable("SeparateEntitiesOne_Inline");
         migrationBuilder.DropTable("SeparateEntitiesOne_SeparateMany_Inner");
         migrationBuilder.DropTable("SeparateEntitiesOne_SeparateOne_Inner");
         migrationBuilder.DropTable("TestEntities_Own_Inline");
         migrationBuilder.DropTable("TestEntities_Own_Inline_Inline");
         migrationBuilder.DropTable("TestEntities_Own_Inline_SeparateMany");
         migrationBuilder.DropTable("TestEntities_Own_Inline_SeparateOne");
         migrationBuilder.DropTable("TestEntities_Own_SeparateMany");
         migrationBuilder.DropTable("TestEntities_Own_SeparateMany_Inline");
         migrationBuilder.DropTable("SeparateEntitiesMany_SeparateEntitiesMany");
         migrationBuilder.DropTable("SeparateEntitiesMany_SeparateEntitiesOne");
         migrationBuilder.DropTable("TestEntities_Own_SeparateOne");
         migrationBuilder.DropTable("TestEntities_Own_SeparateOne_Inline");
         migrationBuilder.DropTable("SeparateEntitiesOne_SeparateMany");
         migrationBuilder.DropTable("SeparateEntitiesOne_SeparateOne");
         migrationBuilder.DropTable("TestEntities_Own_SeparateMany_SeparateMany");
         migrationBuilder.DropTable("TestEntities_Own_SeparateMany_SeparateOne");
         migrationBuilder.DropTable("TestEntities_Own_SeparateOne_SeparateMany");
         migrationBuilder.DropTable("TestEntities_Own_SeparateOne_SeparateOne");
      }
   }
}
