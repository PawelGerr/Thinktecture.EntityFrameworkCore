using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.EntityFrameworkCore.Data;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqlServerTempTableCreatorTests;

[Collection("BulkInsertTempTableAsync")]
// ReSharper disable once InconsistentNaming
public class CreatePrimaryKeyAsync : IntegrationTestsBase
{
   private SqlServerTempTableCreator? _sut;
   private SqlServerTempTableCreator SUT => _sut ??= (SqlServerTempTableCreator)ActDbContext.GetService<ITempTableCreator>();

   public CreatePrimaryKeyAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, true)
   {
   }

   [Fact]
   public async Task Should_create_primary_key_for_keylessType()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();

      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TempTable<int>>(new TempTableCreationOptions
                                                                                                       {
                                                                                                          TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                          PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None
                                                                                                       });

      var entityType = ActDbContext.GetEntityType<TempTable<int>>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      await SUT.CreatePrimaryKeyAsync(ActDbContext, PrimaryKeyPropertiesProviders.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties), tempTableReference.Name);

      var constraints = await AssertDbContext.GetTempTableConstraints<TempTable<int>>().ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

      var keyColumns = await AssertDbContext.GetTempTableKeyColumns<TempTable<int>>().ToListAsync();
      keyColumns.Should().HaveCount(1)
                .And.Subject.First().COLUMN_NAME.Should().Be(nameof(TempTable<int>.Column1));
   }

   [Fact]
   public async Task Should_create_primary_key_for_entityType()
   {
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None
                                                                                                   });

      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      await SUT.CreatePrimaryKeyAsync(ActDbContext, PrimaryKeyPropertiesProviders.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties), tempTableReference.Name);

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
#pragma warning disable 618
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None
                                                                                                   });
#pragma warning restore 618
      var entityType = ArrangeDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      var keyProperties = PrimaryKeyPropertiesProviders.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties);
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
#pragma warning disable 618
      await using var tempTableReference = await ArrangeDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = PrimaryKeyPropertiesProviders.None
                                                                                                   });
      var entityType = ArrangeDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().Select(p => new PropertyWithNavigations(p, Array.Empty<INavigation>())).ToList();
      var keyProperties = PrimaryKeyPropertiesProviders.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties);
      await SUT.CreatePrimaryKeyAsync(ArrangeDbContext, keyProperties, tempTableReference.Name);

      // ReSharper disable once RedundantArgumentDefaultValue
      await SUT.Awaiting(sut => sut.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name, false))
               .Should()
               .ThrowAsync<SqlException>();
   }
}
