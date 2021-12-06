using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

// ReSharper disable once InconsistentNaming
[Collection("BulkInsertTempTableAsync")]
public class BulkInsertValuesIntoTempTableAsync_1_Column : IntegrationTestsBase
{
   public BulkInsertValuesIntoTempTableAsync_1_Column(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_work_with_projection_using_SplitQuery_behavior()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var parentId = new Guid("6EB48130-454B-46BC-9FEE-CDF411F69807");
      var childId = new Guid("17A2014C-F7B2-40E8-82F4-42E754DCAA2D");
      ArrangeDbContext.TestEntities.Add(new TestEntity
                                        {
                                           Id = parentId,
                                           Children = { new() { Id = childId } }
                                        });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { new Guid("6EB48130-454B-46BC-9FEE-CDF411F69807") });

      var query = ActDbContext.TestEntities
                              .AsSplitQuery()
                              .Where(c => tempTable.Query.Any(id => c.Id == id.Column1))
                              .Select(c => new
                                           {
                                              c.Id,
                                              ChildrenIds = c.Children.Select(o => o.Id).ToList()
                                           });

      await query.Awaiting(q => q.ToListAsync())
                 .Should().ThrowAsync<NotSupportedException>()
                 .WithMessage("Temp tables are not supported in queries with QuerySplittingBehavior 'SplitQuery'.");
   }
}