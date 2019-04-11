using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Migrations.DbSchemaAwareMigrationTests
{
   public abstract class OperationsBase : DbSchemaAwareMigrationTestsBase
   {
      protected abstract IReadOnlyList<MigrationOperation> Operations { get; }
      protected abstract Action<MigrationBuilder> Configure { get; set; }

      [Fact]
      public void Should_return_up_migrations_unchanged_if_no_schema_set()
      {
         Configure = builder => builder.AddColumn<string>("Col1", "Table1");

         Operations.Should().HaveCount(1);
         Operations[0].Should().BeOfType<AddColumnOperation>()
                      .And.Subject.As<AddColumnOperation>().Schema.Should().BeNull();
      }

      [Fact]
      public void Should_not_set_schema_if_the_schema_is_set_already()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AddColumn<string>("Col1", "Table1", schema: "CustomSchema");

         Operations[0].As<AddColumnOperation>().Schema.Should().Be("CustomSchema");
      }

      [Fact]
      public void Should_set_schema_on_AddColumnOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AddColumn<string>("Col1", "Table1");

         Operations[0].As<AddColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AlterColumnOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AlterColumn<string>("Col1", "Table1");

         Operations[0].As<AlterColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_RenameColumnOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.RenameColumn("Col1", "Table1", "Col1_New");

         Operations[0].As<RenameColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropColumnOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DropColumn("Table1", "Col1");

         Operations[0].As<DropColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_CreateTable()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.CreateTable("Table1",
                                                    table => new
                                                             {
                                                                Col1 = table.Column<string>()
                                                             },
                                                    constraints: table =>
                                                                 {
                                                                    table.PrimaryKey("PK", t => t.Col1);
                                                                    table.UniqueConstraint("UX", t => t.Col1);
                                                                    table.ForeignKey("FK", t => t.Col1, "OtherTable", "OtherColumn");
                                                                 });

         var op = Operations[0].As<CreateTableOperation>();
         op.Schema.Should().Be("Schema1");
         op.Columns[0].Schema.Should().Be("Schema1");
         op.PrimaryKey.Schema.Should().Be("Schema1");
         op.UniqueConstraints[0].Schema.Should().Be("Schema1");
         op.ForeignKeys[0].Schema.Should().Be("Schema1");
         op.ForeignKeys[0].PrincipalSchema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AlterTableOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AlterTable("Table1");

         Operations[0].As<AlterTableOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_RenameTableOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.RenameTable("Table1", newName: "Table1_New");

         Operations[0].As<RenameTableOperation>().Schema.Should().Be("Schema1");
         Operations[0].As<RenameTableOperation>().NewSchema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropTableOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DropTable("Table1");

         Operations[0].As<DropTableOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AddPrimaryKeyOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AddPrimaryKey("PK", "Table1", "Col1");

         Operations[0].As<AddPrimaryKeyOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropPrimaryKeyOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DropPrimaryKey("PK", "Table1");

         Operations[0].As<DropPrimaryKeyOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AddForeignKeyOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AddForeignKey("FK", "Table1", "Col1", "OtherTable", principalColumn: "OtherCol");

         Operations[0].As<AddForeignKeyOperation>().Schema.Should().Be("Schema1");
         Operations[0].As<AddForeignKeyOperation>().PrincipalSchema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropForeignKeyOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DropForeignKey("FK", "Table1");

         Operations[0].As<DropForeignKeyOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AddUniqueConstraintOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.AddUniqueConstraint("UX", "Table1", "Col1");

         Operations[0].As<AddUniqueConstraintOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropUniqueConstraintOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DropUniqueConstraint("UX", "Table1");

         Operations[0].As<DropUniqueConstraintOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_CreateIndexOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.CreateIndex("IX", "Table1", "Col1");

         Operations[0].As<CreateIndexOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_RenameIndexOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.RenameIndex("IX", "IX_New", "Table");

         Operations[0].As<RenameIndexOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropIndex()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DropIndex("IX", "Table1");

         Operations[0].As<DropIndexOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_InsertDataOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.InsertData("Table1", "Col1", new { });

         Operations[0].As<InsertDataOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_UpdateDataOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.UpdateData("Table1", "Col1", "Key1", "Col1", "Value1");

         Operations[0].As<UpdateDataOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DeleteDataOperation()
      {
         Schema = new DbContextSchema("Schema1");
         Configure = builder => builder.DeleteData("Table1", "Col1", "Key1");

         Operations[0].As<DeleteDataOperation>().Schema.Should().Be("Schema1");
      }
   }
}