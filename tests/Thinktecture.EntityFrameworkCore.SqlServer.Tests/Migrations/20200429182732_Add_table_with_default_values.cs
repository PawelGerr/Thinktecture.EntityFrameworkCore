using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Add_table_with_default_values : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntitiesWithDefaultValues",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                                                  Int = table.Column<int>(nullable: false, defaultValueSql: "1"),
                                                  NullableInt = table.Column<int>(nullable: true, defaultValueSql: "2"),
                                                  String = table.Column<string>(nullable: false, defaultValueSql: "'3'"),
                                                  NullableString = table.Column<string>(nullable: true, defaultValueSql: "'4'")
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesWithDefaultValues", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntitiesWithDefaultValues");
      }
   }
}
