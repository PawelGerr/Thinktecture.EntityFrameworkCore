using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.BulkOperationTableNameOptionsTests;

// ReSharper disable once InconsistentNaming
public class Assignments : SchemaChangingIntegrationTestsBase
{
   public Assignments(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_build_provider_from_table_name_and_keep_model_schema()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      new SqliteBulkInsertOptions { TableName = "X" }.TableNameProvider.GetTableName(entityType)
                                                     .Should().Be(new BulkOperationTableName("X", entityType.GetSchema()));
   }

   [Fact]
   public void Should_build_provider_from_schema_and_keep_model_table_name()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      new SqliteBulkInsertOptions { Schema = "s" }.TableNameProvider.GetTableName(entityType)
                                                  .Should().Be(new BulkOperationTableName(entityType.GetTableName()!, "s"));
   }

   [Fact]
   public void Should_keep_table_name_and_schema_when_both_are_set()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      var options = new SqliteBulkInsertOptions { TableName = "X", Schema = "s" };

      options.TableName.Should().Be("X");
      options.Schema.Should().Be("s");
      options.TableNameProvider.GetTableName(entityType).Should().Be(new BulkOperationTableName("X", "s"));
   }

   [Fact]
   public void Should_throw_for_temp_table_entity_when_only_schema_is_set()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entityType = ActDbContext.GetTempTableEntityType<TestEntityTempTable>();

      new SqliteBulkInsertOptions { Schema = "s" }.TableNameProvider.Invoking(p => p.GetTableName(entityType))
                                                  .Should().Throw<InvalidOperationException>()
                                                  .WithMessage("*temp table entity*");
   }

   [Fact]
   public void Should_replace_provider_when_table_name_is_set_last()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var provider = new SuffixTableNameProvider("");

      var options = new SqliteBulkInsertOptions { TableNameProvider = provider, TableName = "X" };

      options.TableName.Should().Be("X");
      options.TableNameProvider.Should().NotBeSameAs(provider);
      options.TableNameProvider.GetTableName(entityType).Should().Be(new BulkOperationTableName("X", entityType.GetSchema()));
   }

   [Fact]
   public void Should_clear_table_name_and_schema_when_provider_is_set_last()
   {
      var provider = new SuffixTableNameProvider("");

      var options = new SqliteBulkInsertOptions { TableName = "X", Schema = "s", TableNameProvider = provider };

      options.TableName.Should().BeNull();
      options.Schema.Should().BeNull();
      options.TableNameProvider.Should().BeSameAs(provider);
   }

   [Fact]
   public void Should_return_default_provider_when_table_name_and_schema_are_null()
   {
      new SqliteBulkInsertOptions().TableNameProvider.Should().BeSameAs(IBulkOperationTableNameProvider.Default);

      var options = new SqliteBulkInsertOptions { TableName = "X" };
      options.TableName = null;

      options.TableNameProvider.Should().BeSameAs(IBulkOperationTableNameProvider.Default);
   }

   [Fact]
   public void Should_throw_when_table_name_provider_is_set_to_null()
   {
      FluentActions.Invoking(() => new SqliteBulkInsertOptions { TableNameProvider = null! })
                   .Should().Throw<ArgumentNullException>();
   }

   [Fact]
   public void Should_build_provider_from_names_and_ignore_provider_in_ctor()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var provider = new SuffixTableNameProvider("");

      var options = new BulkOperationTableNameOptions("X", "s", provider);

      options.TableName.Should().Be("X");
      options.Schema.Should().Be("s");
      options.TableNameProvider.Should().NotBeSameAs(provider);
      options.TableNameProvider.GetTableName(entityType).Should().Be(new BulkOperationTableName("X", "s"));
   }

   [Fact]
   public void Should_keep_provider_in_ctor_when_no_names_are_given()
   {
      var provider = new SuffixTableNameProvider("");

      var options = new BulkOperationTableNameOptions(tableNameProvider: provider);

      options.TableName.Should().BeNull();
      options.Schema.Should().BeNull();
      options.TableNameProvider.Should().BeSameAs(provider);
   }

   [Fact]
   public void Should_preserve_table_name_and_schema_in_option_copy_ctor()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var provider = new SuffixTableNameProvider("");

      var copyWithNames = new SqliteBulkInsertOptions(new GenericBulkOperationOptions { TableName = "X", Schema = "s" });

      copyWithNames.TableName.Should().Be("X");
      copyWithNames.Schema.Should().Be("s");
      copyWithNames.TableNameProvider.GetTableName(entityType).Should().Be(new BulkOperationTableName("X", "s"));

      new SqliteBulkInsertOptions(new GenericBulkOperationOptions { TableNameProvider = provider })
         .TableNameProvider.Should().BeSameAs(provider);
   }

   [Fact]
   public void Should_build_provider_from_table_name_or_return_default_provider_in_interface_default_member()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkInsertOptions options = new InsertOptionsWithoutProvider { TableName = "X" };

      options.TableNameProvider.GetTableName(entityType).Should().Be(new BulkOperationTableName("X", entityType.GetSchema()));

      ((IBulkInsertOptions)new InsertOptionsWithoutProvider()).TableNameProvider.Should().BeSameAs(IBulkOperationTableNameProvider.Default);
   }

   private sealed class InsertOptionsWithoutProvider : IBulkInsertOptions
   {
      public IEntityPropertiesProvider? PropertiesToInsert => null;
      public string? TableName { get; init; }
      public string? Schema { get; init; }
   }
}
