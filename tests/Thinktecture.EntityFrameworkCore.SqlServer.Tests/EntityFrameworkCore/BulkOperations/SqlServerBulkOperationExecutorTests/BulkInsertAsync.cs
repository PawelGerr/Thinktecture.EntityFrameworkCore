using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqlServerBulkOperationExecutorTests
{
   // ReSharper disable once InconsistentNaming
   public class BulkInsertAsync : IntegrationTestsBase
   {
      private readonly SqlServerBulkOperationExecutor _sut = new SqlServerBulkOperationExecutor();

      public BulkInsertAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_entities()
      {
         var testEntity = new TestEntity
                          {
                             Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                             Name = "Name",
                             Count = 42
                          };
         var testEntities = new[] { testEntity };

         await _sut.BulkInsertAsync(DbContext, testEntities, new SqlBulkInsertOptions());

         var loadedEntities = await DbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(1);
         var loadedEntity = loadedEntities[0];
         loadedEntity.Should().BeEquivalentTo(new TestEntity
                                              {
                                                 Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                                                 Name = "Name",
                                                 Count = 42
                                              });
      }

      [Fact]
      public void Should_throw_when_inserting_queryType_without_providing_tablename()
      {
         ConfigureModel = builder => builder.ConfigureTempTable<int>();

         _sut.Awaiting(sut => sut.BulkInsertAsync(DbContext, new List<TempTable<int>>(), new SqlBulkInsertOptions()))
             .Should().Throw<InvalidOperationException>();
      }
   }
}
