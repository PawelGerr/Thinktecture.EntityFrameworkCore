using System.Reflection;

namespace Thinktecture.Extensions.SqlServerOperationBuilderExtensionsTests;

public class IfExists : ExistTestBase
{
   public IfExists(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public void Should_throw_InvalidOperationException_if_used_with_CreateTable()
   {
      Migration.ConfigureUp = builder => builder.CreateTable("Foo", table => new { Bar = table.Column<int>() }).IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().Throw<InvalidOperationException>();
   }

   [Fact]
   public void Should_not_throw_if_used_with_DropTable_and_table_not_exists()
   {
      Migration.ConfigureUp = builder => builder.DropTable("Foo").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_table_if_used_with_DropTable_and_table_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.DropTable(tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().BeEmpty();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
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
      Migration.ConfigureUp = builder => builder.DropColumn("Foo", "Bar").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_column_if_used_with_DropColumn_and_column_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.DropColumn("Col1", tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");

         ExecuteMigration();

         Context.GetTableColumns(tableName).ToList().Should().HaveCount(1);
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
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
      Migration.ConfigureUp = builder => builder.DropIndex("Foo", "Bar").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_index_if_used_with_DropIndex_and_index_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;

      try
      {
         Migration.ConfigureUp = builder => builder.DropIndex("IX1", tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");
         Context.Database.ExecuteSqlRaw($"CREATE INDEX IX1 ON [{tableName}] (Col1, Col2)");

         ExecuteMigration();

         Context.GetIndexes(tableName).ToList().Should().BeEmpty();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
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
      Migration.ConfigureUp = builder => builder.DropUniqueConstraint("Foo", "Bar").IfExists();

      ExecuteMigration.Invoking(a => a())
                      .Should().NotThrow();
   }

   [Fact]
   public void Should_drop_constraint_if_used_with_DropUniqueConstraint_and_constraint_exists()
   {
      var tableName = MethodBase.GetCurrentMethod()!.Name;
      var constraintName = $"UC_{tableName}";

      try
      {
         Migration.ConfigureUp = builder => builder.DropUniqueConstraint(constraintName, tableName).IfExists();
         Context.Database.ExecuteSqlRaw($"CREATE TABLE [{tableName}] (Col1 INT, Col2 INT)");
         Context.Database.ExecuteSqlRaw($"ALTER TABLE [{tableName}] ADD CONSTRAINT [{constraintName}] UNIQUE (Col1);");

         ExecuteMigration();

         Context.GetUniqueConstraints(constraintName).ToList().Should().BeEmpty();
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"DROP TABLE IF EXISTS [{tableName}]");
      }
   }
}
