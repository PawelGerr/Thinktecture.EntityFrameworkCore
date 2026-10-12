using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.BulkOperationTableNameProviderTests;

// ReSharper disable once InconsistentNaming
public class Default : SchemaChangingIntegrationTestsBase
{
   public Default(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_return_table_and_schema_of_model()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.Default.GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName(entityType.GetTableName()!, entityType.GetSchema()));
   }

   [Fact]
   public void Should_return_table_of_separate_owned_type()
   {
      var ownedType = ActDbContext.GetEntityType<TestEntity_Owns_SeparateOne>().FindNavigation(nameof(TestEntity_Owns_SeparateOne.SeparateEntity))!.TargetEntityType;

      IBulkOperationTableNameProvider.Default.GetTableName(ownedType).Name.Should().Be("SeparateEntitiesOne");
   }

   [Fact]
   public void Should_throw_for_temp_table_entity()
   {
      ConfigureModel = builder => builder.ConfigureTempTableEntity<TestEntityTempTable>(false, TestEntityTempTable.Configure);

      var entityType = ActDbContext.GetTempTableEntityType<TestEntityTempTable>();

      IBulkOperationTableNameProvider.Default.Invoking(p => p.GetTableName(entityType))
                                     .Should().Throw<InvalidOperationException>()
                                     .WithMessage("*configured as a temp table entity*Provide the target table name*");
   }
}
