using System;
using System.Linq;
using FluentAssertions;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.SqliteDbFunctionsExtensionsTests
{
   public class CountDistinct : IntegrationTestsBase
   {
      public CountDistinct(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Computes_count_on_empty_table()
      {
         var result = ActDbContext.TestEntities
                                  .GroupBy(e => e.Name)
                                  .Select(g => g.CountDistinct(e => e.PropertyWithBackingField))
                                  .ToList();
         result.Should().HaveCount(0);
      }

      [Fact]
      public void Computes_count_with_nothing_to_distinct()
      {
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("94B6A0DE-6E45-4A3D-B8E7-08AFB79B81F0"), Name = "A", PropertyWithBackingField = 1 });
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("86C67012-0A72-4759-88F7-2E7452A9FE9C"), Name = "A", PropertyWithBackingField = 2 });
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("D93F85CD-DE3E-4919-B42F-EE39604FC6D7"), Name = "B", PropertyWithBackingField = 3 });
         ArrangeDbContext.SaveChanges();

         var result = ActDbContext.TestEntities
                                  .GroupBy(e => e.Name)
                                  .Select(g => g.CountDistinct(e => e.PropertyWithBackingField))
                                  .ToList();

         result.Should().HaveCount(2);
         result.Should().Contain(2); // A
         result.Should().Contain(1); // B
      }

      [Fact]
      public void Computes_count_with_proper_distinct()
      {
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("94B6A0DE-6E45-4A3D-B8E7-08AFB79B81F0"), Name = "A", PropertyWithBackingField = 1 });
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("86C67012-0A72-4759-88F7-2E7452A9FE9C"), Name = "A", PropertyWithBackingField = 1 });
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("D93F85CD-DE3E-4919-B42F-EE39604FC6D7"), Name = "B", PropertyWithBackingField = 3 });
         ArrangeDbContext.SaveChanges();

         var result = ActDbContext.TestEntities
                                  .GroupBy(e => e.Name)
                                  .Select(g => g.CountDistinct(e => e.PropertyWithBackingField))
                                  .ToList();

         result.Should().HaveCount(2);
         result.Should().AllBeEquivalentTo(1);
      }

      [Fact]
      public void Computes_count_within_new_object()
      {
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("94B6A0DE-6E45-4A3D-B8E7-08AFB79B81F0"), Name = "A", PropertyWithBackingField = 1 });
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("86C67012-0A72-4759-88F7-2E7452A9FE9C"), Name = "A", PropertyWithBackingField = 1 });
         ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("D93F85CD-DE3E-4919-B42F-EE39604FC6D7"), Name = "B", PropertyWithBackingField = 3 });
         ArrangeDbContext.SaveChanges();

         var result = ActDbContext.TestEntities
                                  .GroupBy(e => e.Name)
                                  .Select(g => new
                                               {
                                                  Name = g.Key,
                                                  CountDistinct = g.CountDistinct(e => e.PropertyWithBackingField),
                                                  Count = g.Count()
                                               })
                                  .ToList();

         result.Should().HaveCount(2);
         result.Should().Contain(new { Name = "A", CountDistinct = 1, Count = 2 }!);
         result.Should().Contain(new { Name = "B", CountDistinct = 1, Count = 1 }!);
      }
   }
}
