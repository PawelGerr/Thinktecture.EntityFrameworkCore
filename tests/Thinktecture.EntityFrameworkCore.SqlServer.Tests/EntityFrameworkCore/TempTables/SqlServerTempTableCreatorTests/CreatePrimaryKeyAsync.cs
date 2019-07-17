using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.TempTables.SqlServerTempTableCreatorTests
{
   [Collection("BulkInsertTempTableAsync")]
   // ReSharper disable once InconsistentNaming
   public class CreatePrimaryKeyAsync : IntegrationTestsBase
   {
      private readonly SqlServerTempTableCreator _sut = new SqlServerTempTableCreator();

      public CreatePrimaryKeyAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_create_primary_key_for_queryType()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         var tableName = await DbContext.CreateTempTableAsync<TempTable<int>>();

         await _sut.CreatePrimaryKeyAsync(DbContext, DbContext.GetEntityType<TempTable<int>>(), tableName);

         var constraints = await DbContext.GetTempTableConstraints<TempTable<int>>().ToListAsync();
         constraints.Should().HaveCount(1)
                    .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

         var keyColumns = await DbContext.GetTempTableKeyColumns<TempTable<int>>().ToListAsync();
         keyColumns.Should().HaveCount(1)
                   .And.Subject.First().COLUMN_NAME.Should().Be(nameof(TempTable<int>.Column1));
      }

      [Fact]
      public async Task Should_create_primary_key_for_entityType()
      {
         var tableName = await DbContext.CreateTempTableAsync<TestEntity>();

         await _sut.CreatePrimaryKeyAsync(DbContext, DbContext.GetEntityType<TestEntity>(), tableName);

         var constraints = await DbContext.GetTempTableConstraints<TestEntity>().ToListAsync();
         constraints.Should().HaveCount(1)
                    .And.Subject.First().CONSTRAINT_TYPE.Should().Be("PRIMARY KEY");

         var keyColumns = await DbContext.GetTempTableKeyColumns<TestEntity>().ToListAsync();
         keyColumns.Should().HaveCount(1)
                   .And.Subject.First().COLUMN_NAME.Should().Be(nameof(TestEntity.Id));
      }

      [Fact]
      public async Task Should_not_create_primary_key_if_key_exists_and_checkForExistence_is_true()
      {
         var tableName = await DbContext.CreateTempTableAsync<TestEntity>();
         var entityType = DbContext.GetEntityType<TestEntity>();
         await _sut.CreatePrimaryKeyAsync(DbContext, entityType, tableName);

         _sut.Awaiting(async sut => await sut.CreatePrimaryKeyAsync(DbContext, entityType, tableName, true))
             .Should().NotThrow();
      }

      [Fact]
      public async Task Should_throw_if_key_exists_and_checkForExistence_is_false()
      {
         var tableName = await DbContext.CreateTempTableAsync<TestEntity>();
         var entityType = DbContext.GetEntityType<TestEntity>();
         await _sut.CreatePrimaryKeyAsync(DbContext, entityType, tableName);

         // ReSharper disable once RedundantArgumentDefaultValue
         _sut.Awaiting(async sut => await sut.CreatePrimaryKeyAsync(DbContext, entityType, tableName, false))
             .Should()
             .Throw<SqlException>();
      }
   }
}
