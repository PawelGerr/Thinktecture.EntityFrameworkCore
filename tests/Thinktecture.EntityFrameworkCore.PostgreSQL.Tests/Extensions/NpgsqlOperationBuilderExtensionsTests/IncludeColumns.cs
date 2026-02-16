using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

// ReSharper disable InconsistentNaming
public class IncludeColumns : ExistTestBase
{
   public IncludeColumns(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public void Should_create_index_with_include_columns()
   {
      var tableName = "ic_create_idx_include";

      try
      {
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         Migration.ConfigureUp = builder => builder.CreateIndex("ix_include", tableName, "Col1")
                                                   .IncludeColumns("Col2");

         ExecuteMigration();

         var indexes = Context.GetIndexes(tableName).ToList();
         indexes.Should().HaveCount(1);
         indexes[0].Indexdef.Should().Contain("INCLUDE");
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_if_operation_is_null()
   {
      OperationBuilder<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation>? operation = null;

      var action = () => operation!.IncludeColumns("Col1");

      action.Should().Throw<ArgumentNullException>();
   }

   [Fact]
   public void Should_throw_if_columns_is_empty()
   {
      var operation = new OperationBuilder<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation>(
         new Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation());

      var action = () => operation.IncludeColumns();

      action.Should().Throw<ArgumentException>();
   }
}
