using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.TempTables.NpgsqlTempTableCreatorTests;

// ReSharper disable once InconsistentNaming
public class CreatePrimaryKeyAsync : IntegrationTestsBase
{
   private NpgsqlTempTableCreator SUT => field ??= (NpgsqlTempTableCreator)ActDbContext.GetService<ITempTableCreator>();

   public CreatePrimaryKeyAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_create_primary_key()
   {
      // Use ActDbContext for both temp table creation and PK creation
      // because temp tables are session-scoped in PostgreSQL
      await using var tempTableReference = await ActDbContext.CreateTempTableAsync<KeylessTestEntity>(new TempTableCreationOptions
                                                                                                      {
                                                                                                         TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                         PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                                      });

      var entityType = ActDbContext.GetEntityType<KeylessTestEntity>();
      var allProperties = entityType.GetProperties().ToList();
      await SUT.CreatePrimaryKeyAsync(ActDbContext, IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties), tempTableReference.Name);

      var constraints = await AssertDbContext.GetTempTableConstraints(tempTableReference.Name).ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().ConstraintType.Should().Be("PRIMARY KEY");
   }

   [Fact]
   public async Task Should_create_primary_key_if_key_column_name_contains_braces()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>(typeBuilder => typeBuilder.Property(t => t.Column1).HasColumnName("a{b}"));

      // Use ActDbContext for both temp table creation and PK creation
      // because temp tables are session-scoped in PostgreSQL
      await using var tempTableReference = await ActDbContext.CreateTempTableAsync<TempTable<int>>(new TempTableCreationOptions
                                                                                                   {
                                                                                                      TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                      PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                                   });

      var entityType = ActDbContext.GetTempTableEntityType<TempTable<int>>();
      var allProperties = entityType.GetProperties().ToList();
      await SUT.CreatePrimaryKeyAsync(ActDbContext, IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties), tempTableReference.Name);

      var constraints = await AssertDbContext.GetTempTableConstraints(tempTableReference.Name).ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().ConstraintType.Should().Be("PRIMARY KEY");

      var columns = await AssertDbContext.GetTempTableColumns(tempTableReference.Name).ToListAsync();
      columns.Should().ContainSingle()
             .Which.ColumnName.Should().Be("a{b}");
   }

   [Fact]
   public async Task Should_create_primary_key_with_existence_check()
   {
      // Use ActDbContext for both temp table creation and PK creation
      // because temp tables are session-scoped in PostgreSQL
      await using var tempTableReference = await ActDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                               {
                                                                                                  TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                  PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                               });

      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().ToList();
      var keyProperties = IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties);
      await SUT.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name, true);

      var constraints = await AssertDbContext.GetTempTableConstraints(tempTableReference.Name).ToListAsync();
      constraints.Should().HaveCount(1)
                 .And.Subject.First().ConstraintType.Should().Be("PRIMARY KEY");

      // Creating again with existence check should not throw
      await SUT.Awaiting(sut => sut.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name, true))
               .Should().NotThrowAsync();
   }

   [Fact]
   public async Task Should_throw_if_key_exists_and_checkForExistence_is_false()
   {
      // Use ActDbContext for both temp table creation and PK creation
      // because temp tables are session-scoped in PostgreSQL
      await using var tempTableReference = await ActDbContext.CreateTempTableAsync<TestEntity>(new TempTableCreationOptions
                                                                                               {
                                                                                                  TableNameProvider = DefaultTempTableNameProvider.Instance,
                                                                                                  PrimaryKeyCreation = IPrimaryKeyPropertiesProvider.None
                                                                                               });
      var entityType = ActDbContext.GetEntityType<TestEntity>();
      var allProperties = entityType.GetProperties().ToList();
      var keyProperties = IPrimaryKeyPropertiesProvider.AdaptiveForced.GetPrimaryKeyProperties(entityType, allProperties);
      await SUT.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name);

      // ReSharper disable once RedundantArgumentDefaultValue
      await SUT.Awaiting(sut => sut.CreatePrimaryKeyAsync(ActDbContext, keyProperties, tempTableReference.Name, false))
               .Should()
               .ThrowAsync<PostgresException>();
   }
}
