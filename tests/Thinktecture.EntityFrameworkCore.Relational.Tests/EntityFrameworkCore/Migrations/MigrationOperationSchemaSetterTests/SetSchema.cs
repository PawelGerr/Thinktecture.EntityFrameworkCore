using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Thinktecture.EntityFrameworkCore.Migrations.MigrationOperationSchemaSetterTests
{
   public class SetSchema
   {
      private IDbDefaultSchema? _schema;
      private List<MigrationOperation>? _operations;

      private readonly MigrationBuilder _builder = new MigrationBuilder("provider");

      private List<MigrationOperation> Operations
      {
         get
         {
            if (_operations == null)
            {
               _operations = _builder.Operations;
               new MigrationOperationSchemaSetter().SetSchema(_operations, _schema?.Schema);
            }

            return _operations;
         }
      }

      [Fact]
      public void Should_return_operations_unchanged_if_no_schema_set()
      {
         _builder.AddColumn<string>("Col1", "Table1");

         Operations.Should().HaveCount(1);
         Operations[0].Should().BeOfType<AddColumnOperation>()
                      .And.Subject.As<AddColumnOperation>().Schema.Should().BeNull();
      }

      [Fact]
      public void Should_not_set_schema_if_the_schema_is_set_already()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.AddColumn<string>("Col1", "Table1", schema: "CustomSchema");

         Operations[0].As<AddColumnOperation>().Schema.Should().Be("CustomSchema");
      }

      [Fact]
      public void Should_set_schema_on_AddColumnOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.AddColumn<string>("Col1", "Table1");

         Operations[0].As<AddColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AlterColumnOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.AlterColumn<string>("Col1", "Table1");

         Operations[0].As<AlterColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_RenameColumnOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.RenameColumn("Col1", "Table1", "Col1_New");

         Operations[0].As<RenameColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropColumnOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DropColumn("Table1", "Col1");

         Operations[0].As<DropColumnOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_CreateTable()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.CreateTable("Table1",
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
         _schema = new DbDefaultSchema("Schema1");
         _builder.AlterTable("Table1");

         Operations[0].As<AlterTableOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_RenameTableOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.RenameTable("Table1", newName: "Table1_New");

         Operations[0].As<RenameTableOperation>().Schema.Should().Be("Schema1");
         Operations[0].As<RenameTableOperation>().NewSchema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropTableOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DropTable("Table1");

         Operations[0].As<DropTableOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AddPrimaryKeyOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.AddPrimaryKey("PK", "Table1", "Col1");

         Operations[0].As<AddPrimaryKeyOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropPrimaryKeyOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DropPrimaryKey("PK", "Table1");

         Operations[0].As<DropPrimaryKeyOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AddForeignKeyOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.AddForeignKey("FK", "Table1", "Col1", "OtherTable", principalColumn: "OtherCol");

         Operations[0].As<AddForeignKeyOperation>().Schema.Should().Be("Schema1");
         Operations[0].As<AddForeignKeyOperation>().PrincipalSchema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropForeignKeyOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DropForeignKey("FK", "Table1");

         Operations[0].As<DropForeignKeyOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_AddUniqueConstraintOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.AddUniqueConstraint("UX", "Table1", "Col1");

         Operations[0].As<AddUniqueConstraintOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropUniqueConstraintOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DropUniqueConstraint("UX", "Table1");

         Operations[0].As<DropUniqueConstraintOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_CreateIndexOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.CreateIndex("IX", "Table1", "Col1");

         Operations[0].As<CreateIndexOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_RenameIndexOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.RenameIndex("IX", "IX_New", "Table");

         Operations[0].As<RenameIndexOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DropIndex()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DropIndex("IX", "Table1");

         Operations[0].As<DropIndexOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_InsertDataOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.InsertData("Table1", "Col1", new { });

         Operations[0].As<InsertDataOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_UpdateDataOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.UpdateData("Table1", "Col1", "Key1", "Col1", "Value1");

         Operations[0].As<UpdateDataOperation>().Schema.Should().Be("Schema1");
      }

      [Fact]
      public void Should_set_schema_on_DeleteDataOperation()
      {
         _schema = new DbDefaultSchema("Schema1");
         _builder.DeleteData("Table1", "Col1", "Key1");

         Operations[0].As<DeleteDataOperation>().Schema.Should().Be("Schema1");
      }
   }
}
