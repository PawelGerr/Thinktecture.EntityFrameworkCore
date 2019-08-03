using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.TestDatabaseContext
{
   public class MigrationWithoutSchema : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<string>("Table1", "Col1");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("Table1", "Col1");
      }
   }
}
