using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqlServerTempTableCreatorTests;

// ReSharper disable once InconsistentNaming
public class CreatePrimaryKeyAsync : IntegrationTestsBase
{
   private SqlServerTempTableCreator? _sut;
   private SqlServerTempTableCreator SUT => _sut ??= (SqlServerTempTableCreator)ActDbContext.GetService<ITempTableCreator>();

   public CreatePrimaryKeyAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, true)
   {
      TestCtxProviderBuilder.UseSharedTablesIsolationLevel(IsolationLevel.Serializable);
   }

   [Fact]
   public async Task Should_create_primary_key_for_keylessType()
   {
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<KeylessTestEntity>(new TempTableCreationOptions
                                                                                                          {
                                                                                                             TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                             PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                                          });

      var entityType = ActDbContext.GetEntityType<KeylessTestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      await SUT.CreatePrimaryKeyAsync(ActDbContext, IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties), tempTableReference.Name);

      var constraints = await AssertDbContext.GetTempTableConstraints<KeylessTestEntity>().ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

      var keyColumns = await AssertDbContext.GetTempTableKeyColumns<KeylessTestEntity>().ToListAsync();
      keyColumns.Should().HaveCount(1)
                .And.Subject.First().COLUMN_NAME.Should().Be(nameof(KeylessTestEntity.IntColumn));
   }

   [Fact]
   public async Task Should_create_primary_key_for_entityType()
   {
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                                   });

      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      await SUT.CreatePrimaryKeyAsync(ActDbContext, IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties), tempTableReference.Name);

      var constraints = await AssertDbContext.GetTempTableConstraints<TestEntity>().ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

      var keyColumns = await AssertDbContext.GetTempTableKeyColumns<TestEntity>().ToListAsync();
      keyColumns.Should().HaveCount(1)
                .And.Subject.First().COLUMN_NAME.Should().Be(nameof(TestEntity.Id));
   }

   [Fact]
   public async Task Should_not_create_primary_key_if_key_exists_and_checkForExistence_is_true()
   {
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                                   });
      var entityType = ArrangeDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      var keyProperties = IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties);
      await SUT.CreatePrimaryKeyAsync(ArrangeDbContext, keyProperties, tempTableReference.Name, true);

      var constraints = await AssertDbContext.GetTempTableConstraints<TestEntity>().ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

      await SUT.Awaiting(sut => sut.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name, true))
               .Should().NotThrowAsync();
   }

   [Fact]
   public async Task Should_throw_if_key_exists_and_checkForExistence_is_false()
   {
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                                   });
      var entityType = ArrangeDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      var keyProperties = IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties);
      await SUT.CreatePrimaryKeyAsync(ArrangeDbContext, keyProperties, tempTableReference.Name);

      // ReSharper disable once RedundantArgumentDefaultValue
      // ReSharper disable once AccessToDisposedClosure
      await SUT.Awaiting(sut => sut.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name, false))
               .Should()
               .ThrowAsync<SqlException>();
   }
}
