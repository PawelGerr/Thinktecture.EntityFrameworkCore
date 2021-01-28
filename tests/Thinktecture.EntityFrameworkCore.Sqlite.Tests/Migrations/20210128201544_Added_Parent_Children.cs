using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Added_Parent_Children : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<Guid>("ParentId", "TestEntities", "TEXT", nullable: true);
         migrationBuilder.CreateIndex("IX_TestEntities_ParentId", "TestEntities", "ParentId");

         migrationBuilder.AddForeignKey("FK_TestEntities_TestEntities_ParentId", "TestEntities", "ParentId", "TestEntities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropForeignKey("FK_TestEntities_TestEntities_ParentId", "TestEntities");
         migrationBuilder.DropIndex("IX_TestEntities_ParentId", "TestEntities");
         migrationBuilder.DropColumn("ParentId", "TestEntities");
      }
   }
}
