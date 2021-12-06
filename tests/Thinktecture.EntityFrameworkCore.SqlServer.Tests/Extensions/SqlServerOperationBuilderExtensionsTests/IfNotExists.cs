using System.Reflection;

namespace Thinktecture.Extensions.SqlServerOperationBuilderExtensionsTests;

public class IfNotExists : ExistTestBase
{
   public IfNotExists(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_create_table_if_used_with_CreateTable_and_table_not_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.CreateTable(tableName, table => new { Bar = table.Column<string>() }).IfNotExists();

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
      }
   }

   [Fact]
   public void Should_not_throw_if_used_with_CreateTable_and_table_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.CreateTable(tableName, table => new { Bar = table.Column<string>() }).IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
      }
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_DropTable()
   {
      Migration.ConfigureUp = builder => builder.DropTable("Foo").IfNotExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_add_column_if_used_with_AddColumn_and_column_not_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.AddColumn<int>("Col2", tableName).IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT)");

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
      }
   }

   [Fact]
   public void Should_not_throw_if_used_with_AddColumn_and_column_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.AddColumn<int>("Col2", tableName).IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
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
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.CreateIndex("IX1", tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");
         Context.Database.ExecuteSqlRaw($"CREATE INDEX IX1 ON [{tableName}] (Col1, Col2)");

         ExecuteMigration();

         var indexes = Context.GetIndexes(tableName).ToList();
         indexes.Should().HaveCount(1);
         Context.GetIndexColumns(tableName, indexes[0].Index_Id).ToList().Should().HaveCount(2);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
      }
   }

   [Fact]
   public void Should_create_index_if_used_with_CreateIndex_and_index_not_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.CreateIndex("IX1", tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");

         ExecuteMigration();

         var indexes = Context.GetIndexes(tableName).ToList();
         indexes.Should().HaveCount(1);
         Context.GetIndexColumns(tableName, indexes[0].Index_Id).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
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
      var tableName = MethodBase.GetCurrentMethod()!.Name;
      var constraintName = $"UC_{tableName}";

      try
      {
         Migration.ConfigureUp = builder => builder.AddUniqueConstraint(constraintName, tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");
         Context.Database.ExecuteSqlRaw($"ALTER TABLE [{tableName}] ADD CONSTRAINT [{constraintName}] UNIQUE (Col1);");

         ExecuteMigration.Invoking(a => a())
                         .Should().NotThrow();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
      }
   }

   [Fact]
   public void Should_create_constraint_if_used_with_AddUniqueConstraint_and_constraint_not_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;
      var constraintName = $"UC_{tableName}";

      try
      {
         Migration.ConfigureUp = builder => builder.AddUniqueConstraint(constraintName, tableName, "Col1").IfNotExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");

         ExecuteMigration();

         Context.GetUniqueConstraints(constraintName).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
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
