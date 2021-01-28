using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.QueryableExtensionsTests
{
   // ReSharper disable InconsistentNaming
   public class BulkDelete : IntegrationTestsBase
   {
      public BulkDelete(ITestOutputHelper testOutputHelper)
         : base(testOutputHelper)
      {
      }

      [Fact]
      public void Should_delete_all_entities()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         ArrangeDbContext.SaveChanges();

         var affectedRows = ActDbContext.TestEntities.BulkDelete();
         affectedRows.Should().Be(2);

         var loadedEntities = AssertDbContext.TestEntities.ToList();
         loadedEntities.Should().BeEmpty();
      }

      [Fact]
      public void Should_ignore_orderby()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         ArrangeDbContext.SaveChanges();

         var affectedRows = ActDbContext.TestEntities.OrderBy(e => e.Id).BulkDelete();
         affectedRows.Should().Be(2);

         var loadedEntities = AssertDbContext.TestEntities.ToList();
         loadedEntities.Should().BeEmpty();
      }

      [Fact]
      public void Should_throw_if_there_are_more_than_1_table()
      {
         var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
         var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
         ArrangeDbContext.Add(parent);
         ArrangeDbContext.Add(child);
         ArrangeDbContext.SaveChanges();

         ActDbContext.TestEntities.SelectMany(e => e.Children)
                     .Invoking(q => q.BulkDelete())
                     .Should().Throw<NotSupportedException>().WithMessage("SQLite supports only 1 outermost table in a DELETE statement.");

         var loadedEntities = AssertDbContext.TestEntities.ToList();
         loadedEntities.Should().HaveCount(2);
      }

      [Fact]
      public void Should_delete_entities_matching_where_clause()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), Name = "Test" });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         ArrangeDbContext.SaveChanges();

         var affectedRows = ActDbContext.TestEntities
                                        .Where(e => e.Name == "Test")
                                        .BulkDelete();
         affectedRows.Should().Be(1);

         var loadedEntities = AssertDbContext.TestEntities.ToList();
         loadedEntities.Should().BeEquivalentTo(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
      }

      [Fact]
      public void Should_throw_if_take_is_present()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         ArrangeDbContext.SaveChanges();

         ActDbContext.TestEntities.Take(2).Invoking(q => q.BulkDelete())
                     .Should().Throw<NotSupportedException>().WithMessage("A TOP/LIMIT clause (i.e. Take(x)) is not supported in a DELETE statement.");

         var loadedEntities = AssertDbContext.TestEntities.ToList();
         loadedEntities.Should().HaveCount(2);
      }

      [Fact]
      public void Should_throw_if_skip_is_present()
      {
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
         ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
         ArrangeDbContext.SaveChanges();

         ActDbContext.TestEntities.Skip(1).Invoking(q => q.BulkDelete())
                     .Should().Throw<NotSupportedException>("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

         var loadedEntities = AssertDbContext.TestEntities.ToList();
         loadedEntities.Should().HaveCount(2);
      }
   }
}
