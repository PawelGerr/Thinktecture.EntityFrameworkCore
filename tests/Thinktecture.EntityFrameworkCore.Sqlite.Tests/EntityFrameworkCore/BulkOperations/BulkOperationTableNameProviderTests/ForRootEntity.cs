using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.BulkOperationTableNameProviderTests;

// ReSharper disable once InconsistentNaming
public class ForRootEntity : SchemaChangingIntegrationTestsBase
{
   public ForRootEntity(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public void Should_return_fixed_name_and_null_schema_for_root()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.ForRootEntity("X").GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName("X", null));
   }

   [Fact]
   public void Should_return_fixed_name_and_schema_for_root()
   {
      var entityType = ActDbContext.GetEntityType<TestEntity>();

      IBulkOperationTableNameProvider.ForRootEntity("X", "s").GetTableName(entityType)
                                     .Should().Be(new BulkOperationTableName("X", "s"));
   }

   [Fact]
   public void Should_throw_for_separate_owned_type()
   {
      var ownedType = ActDbContext.GetEntityType<TestEntity_Owns_SeparateOne>().FindNavigation(nameof(TestEntity_Owns_SeparateOne.SeparateEntity))!.TargetEntityType;

      IBulkOperationTableNameProvider.ForRootEntity("X").Invoking(p => p.GetTableName(ownedType))
                                     .Should().Throw<InvalidOperationException>()
                                     .WithMessage("*'TableName'*'TableNameProvider'*")
                                     .And.Message.Should().Contain(nameof(TestEntity_Owns_SeparateOne.SeparateEntity));
   }

   [Fact]
   public void Should_throw_for_empty_name_or_empty_schema()
   {
      FluentActions.Invoking(() => IBulkOperationTableNameProvider.ForRootEntity(" "))
                   .Should().Throw<ArgumentException>();

      FluentActions.Invoking(() => IBulkOperationTableNameProvider.ForRootEntity("X", " "))
                   .Should().Throw<ArgumentException>();
   }
}
