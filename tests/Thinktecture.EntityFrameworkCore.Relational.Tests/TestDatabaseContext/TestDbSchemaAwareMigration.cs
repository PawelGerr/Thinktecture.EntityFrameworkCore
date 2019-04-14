using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbSchemaAwareMigration : DbSchemaAwareMigration
   {
      public new string Schema => base.Schema;

      public Action<MigrationBuilder> ConfigureUp { get; set; }
      public Action<MigrationBuilder> ConfigureDown { get; set; }

      /// <inheritdoc />
      public TestDbSchemaAwareMigration([CanBeNull] IDbContextSchema schema)
         : base(schema)
      {
      }

      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         ConfigureUp?.Invoke(migrationBuilder);
      }

      /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
      {
         ConfigureDown?.Invoke(migrationBuilder);
      }
   }
}
