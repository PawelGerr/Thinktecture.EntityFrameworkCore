namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

// ReSharper disable InconsistentNaming
public class IfExists : ExistTestBase
{
   public IfExists(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_CreateTable()
   {
      Migration.ConfigureUp = builder => builder.CreateTable("foo", table => new { Bar = table.Column<int>() }).IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_DropTable_and_table_not_exists()
   {
      Migration.ConfigureUp = builder => builder.DropTable("foo").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_table_if_used_with_DropTable_and_table_exists()
   {
      var tableName = "ie_drop_tbl_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.DropTable(tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().BeEmpty();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_AddColumn()
   {
      Migration.ConfigureUp = builder => builder.AddColumn<int>("Foo", "Bar").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_DropColumn_and_column_not_exists()
   {
      var tableName = "ie_drop_col_not_exists";

      try
      {
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col2" integer NOT NULL) """);

         Migration.ConfigureUp = builder => builder.DropColumn("Col1", tableName).IfExists();

         ExecuteMigration.Invoking(a => a())
                         .Should().NotThrow();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_drop_column_if_used_with_DropColumn_and_column_exists()
   {
      var tableName = "ie_drop_col_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.DropColumn("Col1", tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_CreateIndex()
   {
      Migration.ConfigureUp = builder => builder.CreateIndex("Foo", "Bar", "Col1").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_DropIndex_and_index_not_exists()
   {
      Migration.ConfigureUp = builder => builder.DropIndex("foo", "bar").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_index_if_used_with_DropIndex_and_index_exists()
   {
      var tableName = "ie_drop_idx_exists";

      try
      {
         Migration.ConfigureUp = builder => builder.DropIndex("ix1", tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);
         Context.Database.ExecuteSqlRaw($"""CREATE INDEX "ix1" ON "{tableName}" ("Col1", "Col2") """);

         ExecuteMigration();

         Context.GetIndexes(tableName).ToList().Should().BeEmpty();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_AddUniqueConstraint()
   {
      Migration.ConfigureUp = builder => builder.AddUniqueConstraint("Foo", "Bar", "Col1").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_DropUniqueConstraint_and_constraint_not_exists()
   {
      Migration.ConfigureUp = builder => builder.DropUniqueConstraint("foo", "bar").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_constraint_if_used_with_DropUniqueConstraint_and_constraint_exists()
   {
      var tableName = "ie_drop_uc_exists";
      var constraintName = $"uc_{tableName}";

      try
      {
         Migration.ConfigureUp = builder => builder.DropUniqueConstraint(constraintName, tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL, "Col2" integer NOT NULL) """);
         Context.Database.ExecuteSqlRaw($"""ALTER TABLE "{tableName}" ADD CONSTRAINT "{constraintName}" UNIQUE ("Col1") """);

         ExecuteMigration();

         Context.GetUniqueConstraints(constraintName).ToList().Should().BeEmpty();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }
}
