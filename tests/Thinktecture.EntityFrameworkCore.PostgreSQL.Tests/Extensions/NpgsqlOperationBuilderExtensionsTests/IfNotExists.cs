namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

// ReSharper disable InconsistentNaming
public class IfNotExists : ExistTestBase
{
   public IfNotExists(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public void Should_create_table_if_used_with_CreateTable_and_table_not_exists()
   {
      var tableName = "ine_create_tbl_not_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.CreateTable(tableName, table => new { Bar = table.Column<string>() }).IfNotExists();

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_not_throw_if_used_with_CreateTable_and_table_exists()
   {
      var tableName = "ine_create_tbl_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.CreateTable(tableName, table => new { Bar = table.Column<string>() }).IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_DropTable()
   {
      Migration.ConfigureUp = builder => builder.DropTable("foo").IfNotExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_add_column_if_used_with_AddColumn_and_column_not_exists()
   {
      var tableName = "ine_add_col_not_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.AddColumn<int>("Col2", tableName).IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL) """);

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_not_throw_if_used_with_AddColumn_and_column_exists()
   {
      var tableName = "ine_add_col_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.AddColumn<int>("Col2", tableName).IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_DropColumn()
   {
      Migration.ConfigureUp = builder => builder.DropColumn("Foo", "Bar").IfNotExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_CreateIndex_and_index_exists()
   {
      var tableName = "ine_create_idx_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.CreateIndex("ix1", tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);
         Context.Database.ExecuteSqlRaw($"""CREATE INDEX "ix1" ON "{tableName}" ("Col1", "Col2") """);

         ExecuteMigration();

         var indexes = Context.GetIndexes(tableName).ToList();
         indexes.Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_create_index_if_used_with_CreateIndex_and_index_not_exists()
   {
      var tableName = "ine_create_idx_not_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.CreateIndex("ix1", tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         ExecuteMigration();

         var indexes = Context.GetIndexes(tableName).ToList();
         indexes.Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_DropIndex()
   {
      Migration.ConfigureUp = builder => builder.DropIndex("Foo", "Bar").IfNotExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_AddUniqueConstraint_and_constraint_exists()
   {
      var tableName = "ine_add_uc_exists";
      var constraintName = $"uc_{tableName}";

      try
      {
         Migration.ConfigureUp = builder => builder.AddUniqueConstraint(constraintName, tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);
         Context.Database.ExecuteSqlRaw($"""ALTER TABLE "{tableName}" ADD CONSTRAINT "{constraintName}" UNIQUE ("Col1") """);

         ExecuteMigration.Invoking(a => a())
                         .Should().NotThrow();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_create_constraint_if_used_with_AddUniqueConstraint_and_constraint_not_exists()
   {
      var tableName = "ine_add_uc_not_exists";
      var constraintName = $"uc_{tableName}";

      try
      {
         Migration.ConfigureUp = builder => builder.AddUniqueConstraint(constraintName, tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         ExecuteMigration();

         Context.GetUniqueConstraints(constraintName).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_DropUniqueConstraint()
   {
      Migration.ConfigureUp = builder => builder.DropUniqueConstraint("Foo", "Bar").IfNotExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }
}
