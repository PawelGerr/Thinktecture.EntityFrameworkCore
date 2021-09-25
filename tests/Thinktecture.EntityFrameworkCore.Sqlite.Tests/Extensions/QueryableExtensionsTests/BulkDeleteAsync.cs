using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.QueryableExtensionsTests
{
   // ReSharper disable InconsistentNaming
   public class BulkDeleteAsync : IntegrationTestsBase
   {
      public BulkDeleteAsync(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public async Task Should_delete_all_entities()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         await ArrangeDbContext.SaveChangesAsync();

         var affectedRows = await ActDbContext.TestEntities.BulkDeleteAsync();
         affectedRows.Should().Be(2);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().BeEmpty();
      }

      [Fact]
      public async Task Should_ignore_orderby()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         await ArrangeDbContext.SaveChangesAsync();

         var affectedRows = await ActDbContext.TestEntities.OrderBy(e => e.Id).BulkDeleteAsync();
         affectedRows.Should().Be(2);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().BeEmpty();
      }

      [Fact]
      public async Task Should_throw_if_there_are_more_than_1_table()
      {
         var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
         var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
         ArrangeDbContext.Add(parent);
         ArrangeDbContext.Add(child);
         await ArrangeDbContext.SaveChangesAsync();

         await ActDbContext.TestEntities.SelectMany(e => e.Children)
                           .Awaiting(q => q.BulkDeleteAsync())
                           .Should().ThrowAsync<NotSupportedException>().WithMessage("SQLite supports only 1 outermost table in a DELETE statement.");

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2);
      }

      [Fact]
      public async Task Should_delete_entities_matching_where_clause()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), Name = "Test" });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         await ArrangeDbContext.SaveChangesAsync();

         var affectedRows = await ActDbContext.TestEntities
                                              .Where(e => e.Name == "Test")
                                              .BulkDeleteAsync();
         affectedRows.Should().Be(1);

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") } });
      }

      [Fact]
      public async Task Should_throw_if_take_is_present()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         await ArrangeDbContext.SaveChangesAsync();

         await ActDbContext.TestEntities.Take(2).Awaiting(q => q.BulkDeleteAsync())
                           .Should().ThrowAsync<NotSupportedException>().WithMessage("A TOP/LIMIT clause (i.e. Take(x)) is not supported in a DELETE statement.");

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2);
      }

      [Fact]
      public async Task Should_throw_if_skip_is_present()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         await ArrangeDbContext.SaveChangesAsync();

         await ActDbContext.TestEntities.Skip(1).Awaiting(q => q.BulkDeleteAsync())
                           .Should().ThrowAsync<NotSupportedException>().WithMessage("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

         var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
         loadedEntities.Should().HaveCount(2);
      }
   }
}
