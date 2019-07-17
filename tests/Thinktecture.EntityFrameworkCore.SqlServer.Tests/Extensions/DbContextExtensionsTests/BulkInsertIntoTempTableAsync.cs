using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests
{
   // ReSharper disable once InconsistentNaming
   [Collection("BulkInsertTempTableAsync")]
   public class BulkInsertIntoTempTableAsync : IntegrationTestsBase
   {
      public BulkInsertIntoTempTableAsync([NotNull] ITestOutputHelper testOutputHelper)
         : base(testOutputHelper, true)
      {
      }

      [Fact]
      public async Task Should_insert_queryType()
      {
         ConfigureModel = builder => builder.ConfigureCustomTempTable<CustomTempTable>().Property(t => t.Column2).HasMaxLength(100).IsRequired();

         var entities = new List<CustomTempTable> { new CustomTempTable(1, "value") };
         var query = await DbContext.BulkInsertIntoTempTableAsync(entities);

         var tempTable = await query.ToListAsync();
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new CustomTempTable(1, "value"));
      }

      [Fact]
      public async Task Should_insert_entityType_without_touching_real_table()
      {
         var entity = new TestEntity
                      {
                         Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                         Name = "Name",
                         Count = 42
                      };
         DbContext.TestEntities.Add(entity);
         await DbContext.SaveChangesAsync();

         var entities = new List<TestEntity> { entity };
         var query = await DbContext.BulkInsertIntoTempTableAsync(entities);

         var tempTable = await query.ToListAsync();
         tempTable.Should()
                  .HaveCount(1).And
                  .BeEquivalentTo(new TestEntity
                                  {
                                     Id = new Guid("577BFD36-21BC-4F9E-97B4-367B8F29B730"),
                                     Name = "Name",
                                     Count = 42
                                  });
      }
   }
}
