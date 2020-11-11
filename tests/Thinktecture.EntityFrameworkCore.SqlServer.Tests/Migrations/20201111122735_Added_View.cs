using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture.Migrations
{
   public partial class Added_View : Migration, IDbDefaultSchema
   {
      public string? Schema { get; }

      public Added_View(IDbDefaultSchema? schema)
      {
         Schema = schema?.Schema;
      }

      protected override void Up(MigrationBuilder migrationBuilder)
      {
         var schema = GetEscapedSchema();

         migrationBuilder.Sql($@"
CREATE VIEW {schema}[TestView]
AS
SELECT Id, Name
FROM {schema}[TestEntities]");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         var schema = GetEscapedSchema();

         migrationBuilder.Sql($"DROP VIEW {schema}[TestView]");
      }

      private string? GetEscapedSchema()
      {
         return String.IsNullOrWhiteSpace(Schema) ? null : $"[{Schema}].";
      }
   }
}
