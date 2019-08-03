using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.TestDatabaseContext
{
   public class MigrationWithSchema : Migration, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public MigrationWithSchema(IDbContextSchema schema)
      {
         Schema = schema?.Schema;
      }

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
