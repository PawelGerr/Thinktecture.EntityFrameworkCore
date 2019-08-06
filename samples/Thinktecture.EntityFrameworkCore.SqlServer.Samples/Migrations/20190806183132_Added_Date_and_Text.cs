using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once InconsistentNaming
   public partial class Added_Date_and_Text : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<DateTime>("Date", "Orders", nullable: false, defaultValue: DateTime.Now);
         migrationBuilder.AddColumn<string>("Text", "Orders", nullable: true);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("Date", "Orders");
         migrationBuilder.DropColumn("Text", "Orders");
      }
   }
}
