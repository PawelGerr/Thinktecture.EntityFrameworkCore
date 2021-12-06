using Microsoft.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Add_table_with_dotnet_default_values : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntitiesWithDotnetDefaultValues",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(nullable: false, defaultValue: new Guid("0b151271-79bb-4f6c-b85f-e8f61300ff1b")),
                                                  Int = table.Column<int>(nullable: false, defaultValue: 1),
                                                  NullableInt = table.Column<int>(nullable: true, defaultValue: 2),
                                                  String = table.Column<string>(nullable: false, defaultValue: "3"),
                                                  NullableString = table.Column<string>(nullable: true, defaultValue: "4")
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntitiesWithDotnetDefaultValues", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntitiesWithDotnetDefaultValues");
      }
   }
}
