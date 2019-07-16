using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.Migrations
{
   // ReSharper disable once UnusedMember.Global
   // ReSharper disable once InconsistentNaming
   public partial class Add_shadow_and_private_properties : DbSchemaAwareMigration
   {
      /// <inheritdoc />
      public Add_shadow_and_private_properties([CanBeNull] IDbContextSchema schema)
         : base(schema)
      {
      }

      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<int>("PropertyWithBackingField", "TestEntities");
         migrationBuilder.AddColumn<string>("ShadowProperty", "TestEntities", maxLength: 50, nullable: true);
         migrationBuilder.AddColumn<int>("_privateField", "TestEntities");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn("PropertyWithBackingField", "TestEntities");
         migrationBuilder.DropColumn("ShadowProperty", "TestEntities");
         migrationBuilder.DropColumn("_privateField", "TestEntities");
      }
   }
}
