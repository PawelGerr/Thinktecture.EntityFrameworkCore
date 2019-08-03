using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.Migrations;

namespace Thinktecture.TestDatabaseContext
{
   public class TestDbSchemaAwareMigration : Migration
   {
      public string Schema { get; }

      public Action<MigrationBuilder> ConfigureUp { get; set; }
      public Action<MigrationBuilder> ConfigureDown { get; set; }

      /// <inheritdoc />
      public TestDbSchemaAwareMigration([CanBeNull] IDbContextSchema schema)
      {
         Schema = schema?.Schema;
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
