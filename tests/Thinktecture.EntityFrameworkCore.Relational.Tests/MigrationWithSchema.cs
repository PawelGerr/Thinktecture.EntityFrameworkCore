using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;

namespace Thinktecture
{
   public class MigrationWithSchema : Migration, IDbContextSchema
   {
      /// <inheritdoc />
      public string Schema { get; }

      public MigrationWithSchema(IDbContextSchema schema)
      {
         Schema = schema?.Schema;
      }

      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
      }
   }
}
