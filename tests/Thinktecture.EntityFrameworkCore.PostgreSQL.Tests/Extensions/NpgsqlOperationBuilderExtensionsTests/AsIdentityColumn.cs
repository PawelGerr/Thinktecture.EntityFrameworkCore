using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Thinktecture.Extensions.NpgsqlOperationBuilderExtensionsTests;

// ReSharper disable InconsistentNaming
public class AsIdentityColumn : ExistTestBase
{
   public AsIdentityColumn(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public void Should_create_column_as_identity()
   {
      var tableName = "aic_identity_col";

      try
      {
         Context.Database.ExecuteSqlRaw($"""CREATE TABLE "{tableName}" ("Col1" integer NOT NULL) """);

         Migration.ConfigureUp = builder => builder.AddColumn<int>("IdCol", tableName).AsIdentityColumn();

         ExecuteMigration();

         var identityColumns = Context.GetIdentityColumns(tableName).ToList();
         identityColumns.Should().ContainSingle()
                        .Which.Column_Name.Should().Be("IdCol");
      }
      finally
      {
         Context.Database.ExecuteSqlRaw($"""DROP TABLE IF EXISTS "{tableName}" """);
      }
   }

   [Fact]
   public void Should_throw_if_operation_is_null()
   {
      OperationBuilder<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>? operation = null;

      var action = () => operation!.AsIdentityColumn();

      action.Should().Throw<ArgumentNullException>();
   }
}
