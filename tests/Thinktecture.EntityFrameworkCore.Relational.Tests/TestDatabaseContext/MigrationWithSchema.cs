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

      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
      }
   }
}
