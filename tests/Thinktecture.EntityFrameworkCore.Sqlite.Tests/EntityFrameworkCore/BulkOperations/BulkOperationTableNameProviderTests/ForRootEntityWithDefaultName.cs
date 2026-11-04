using Microsoft.EntityFrameworkCore.Diagnostics;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.BulkOperationTableNameProviderTests;

// ReSharper disable once InconsistentNaming
public class ForRootEntityWithDefaultName : SchemaChangingIntegrationTestsBase
{
   public ForRootEntityWithDefaultName(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_return_model_name_and_fixed_schema()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultName("s").GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName(entityType.GetTableName()!, "s"));
   }

   [Fact]
   public void Should_return_model_name_and_null_schema()
   {
      ConfigureModel = builder => builder.HasDefaultSchema("s");
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));

      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(null).GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName(entityType.GetTableName()!, null));
   }

   [Fact]
   public void Should_throw_for_temp_table_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entityType = ActDbContext.GetTempTableEntityType<TestEntityTempTable>();

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultName("s").Invoking(p => p.GetTableName(entityType))
                                     .Should().Throw<InvalidOperationException>()
                                     .WithMessage("*configured as a temp table entity*");
   }

   [Fact]
   public void Should_throw_for_separate_owned_type()
   {
      var ownedType = ActDbContext.GetEntityType<TestEntity_Owns_SeparateOne>().FindNavigation(nameof(TestEntity_Owns_SeparateOne.SeparateEntity))!.TargetEntityType;

      IBulkOperationTableNameProvider.ForRootEntityWithDefaultName("s").Invoking(p => p.GetTableName(ownedType))
                                     .Should().Throw<InvalidOperationException>()
                                     .WithMessage("*'TableName'*'TableNameProvider'*")
                                     .And.Message.Should().Contain(nameof(TestEntity_Owns_SeparateOne.SeparateEntity));
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   public void Should_throw_for_empty_schema(string schema)
   {
      FluentActions.Invoking(() => IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(schema))
                   .Should().Throw<ArgumentException>();
   }

   [Fact]
   public void Should_not_throw_for_null_schema()
   {
      FluentActions.Invoking(() => IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(null))
                   .Should().NotThrow();
   }
}
