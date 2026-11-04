using Microsoft.EntityFrameworkCore.Diagnostics;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.BulkOperationTableNameProviderTests;

// ReSharper disable once InconsistentNaming
public class ForRootEntityWithDefaultSchema : SchemaChangingIntegrationTestsBase
{
   public ForRootEntityWithDefaultSchema(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_return_fixed_name_and_schema_of_ToTable()
   {
      ConfigureModel = builder => builder.Entity<TestEntity>().ToTable("TestEntities", "s");
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("X").GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName("X", "s"));
   }

   [Fact]
   public void Should_return_fixed_name_and_default_schema_of_model()
   {
      ConfigureModel = builder => builder.HasDefaultSchema("s");
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("X").GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName("X", "s"));
   }

   [Fact]
   public void Should_return_fixed_name_and_null_schema_when_model_has_no_schema()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();
      entityType.GetSchema().Should().BeNull();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("X").GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName("X", null));
   }

   [Fact]
   public void Should_throw_for_separate_owned_type()
   {
      var ownedType = ActDbContext.GetEntityType<TestEntity_Owns_SeparateOne>().FindNavigation(nameof(TestEntity_Owns_SeparateOne.SeparateEntity))!.TargetEntityType;

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("X").Invoking(p => p.GetTableName(ownedType))
                                     .Should().Throw<InvalidOperationException>()
                                     .WithMessage("*'TableName'*'TableNameProvider'*")
                                     .And.Message.Should().Contain(nameof(TestEntity_Owns_SeparateOne.SeparateEntity));
   }

   [Fact]
   public void Should_throw_for_temp_table_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entityType = ActDbContext.GetTempTableEntityType<TestEntityTempTable>();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema("X").Invoking(p => p.GetTableName(entityType))
                                     .Should().Throw<InvalidOperationException>()
                                     .WithMessage("*temp table entity*ForRootEntity(tableName, schema)*");
   }

   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData(" ")]
   public void Should_throw_for_empty_name(string? tableName)
   {
      FluentActions.Invoking(() => IBulkOperationTableNameProvider.ForRootEntityWithDefaultSchema(tableName!))
                   .Should().Throw<ArgumentException>();
   }
}
