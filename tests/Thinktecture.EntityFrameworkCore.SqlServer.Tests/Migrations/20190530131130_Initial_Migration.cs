using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   public partial class Initial_Migration : DbSchemaAwareMigration
   {
      /// <inheritdoc />
      public Initial_Migration([CanBeNull] IDbContextSchema schema)
         : base(schema)
      {
      }

      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable("TestEntities",
                                      table => new
                                               {
                                                  Id = table.Column<Guid>(),
                                                  Name = table.Column<string>(nullable: true),
                                                  Count = table.Column<int>()
                                               },
                                      constraints: table => table.PrimaryKey("PK_TestEntities", x => x.Id));
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable("TestEntities");
      }
   }
}
