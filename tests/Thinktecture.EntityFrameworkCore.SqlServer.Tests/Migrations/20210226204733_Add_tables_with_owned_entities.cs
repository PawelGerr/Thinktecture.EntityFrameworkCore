using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Add_tables_with_owned_entities : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntitiesOwningInlineEntity",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  InlineEntity_StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  InlineEntity_IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesOwningInlineEntity", x => x.Id));

         migrationBuilder.CreateTable("TestEntitiesOwningManyEntities",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesOwningManyEntities", x => x.Id));

         migrationBuilder.CreateTable("TestEntitiesOwningOneSeparateEntity",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>("uniqueidentifier", nullable: false)
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesOwningOneSeparateEntity", x => x.Id));

         migrationBuilder.CreateTable("SeparateEntities_Many",
                                      table => new
                                               {
                                                  TestEntityOwningManyEntitiesId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  Id = table.Column<int>("int", nullable: false)
                                                            .Annotation("SqlServer:Identity", "1, 1"),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntities_Many", x => new { x.TestEntityOwningManyEntitiesId, x.Id });
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntities_Many_TestEntitiesOwningManyEntities_TestEntityOwningManyEntitiesId",
                                                                       x => x.TestEntityOwningManyEntitiesId,
                                                                       "TestEntitiesOwningManyEntities",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });

         migrationBuilder.CreateTable("SeparateEntities_One",
                                      table => new
                                               {
                                                  TestEntityOwningOneSeparateEntityId = table.Column<Guid>("uniqueidentifier", nullable: false),
                                                  StringColumn = table.Column<string>("nvarchar(max)", nullable: true),
                                                  IntColumn = table.Column<int>("int", nullable: false)
                                               },
                                      constraints: table =>
                                                   {
                                                      table.PrimaryKey("PK_SeparateEntities_One", x => x.TestEntityOwningOneSeparateEntityId);
                                                      table.ForeignKey(
                                                                       "FK_SeparateEntities_One_TestEntitiesOwningOneSeparateEntity_TestEntityOwningOneSeparateEntityId",
                                                                       x => x.TestEntityOwningOneSeparateEntityId,
                                                                       "TestEntitiesOwningOneSeparateEntity",
                                                                       "Id",
                                                                       onDelete: ReferentialAction.Cascade);
                                                   });
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("SeparateEntities_Many");
         migrationBuilder.DropTable("SeparateEntities_One");
         migrationBuilder.DropTable("TestEntitiesOwningInlineEntity");
         migrationBuilder.DropTable("TestEntitiesOwningManyEntities");
         migrationBuilder.DropTable("TestEntitiesOwningOneSeparateEntity");
      }
   }
}
