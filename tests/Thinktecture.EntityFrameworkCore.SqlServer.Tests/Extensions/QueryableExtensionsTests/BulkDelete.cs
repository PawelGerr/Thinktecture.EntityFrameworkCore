using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;
using Xunit;
using Xunit.Abstractions;

namespace Thinktecture.Extensions.QueryableExtensionsTests;

// ReSharper disable InconsistentNaming
public class BulkDelete : IntegrationTestsBase
{
   public BulkDelete(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, true)
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
   public void Should_delete_projected_entities()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .SelectMany(e => e.Children)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") } });
   }

   [Fact]
   public void Should_ignore_included_entities()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Include(e => e.Children)
                                     .Where(i => i.ParentId != null)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") } });
   }

   [Fact]
   public void Should_throw_on_innerjoin_projecting_multiple_tables()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      ActDbContext.TestEntities
                  .Join(ActDbContext.TestEntities, p => p.Id, c => c.ParentId, (p, c) => new { p, c })
                  .Invoking(q => q.BulkDelete())
                  .Should().Throw<NotSupportedException>().WithMessage("The provided query is referencing more than 1 table. Found tables: [TestEntities AS t, TestEntities AS t0].");
   }

   [Fact]
   public void Should_throw_on_leftjoin_projecting_multiple_tables()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      ActDbContext.TestEntities
                  .LeftJoin(ActDbContext.TestEntities, p => p.Id, c => c.ParentId)
                  .Invoking(q => q.BulkDelete())
                  .Should().Throw<NotSupportedException>().WithMessage("The provided query is referencing more than 1 table. Found tables: [TestEntities AS t, TestEntities AS t0].");
   }

   [Fact]
   public void Should_delete_projected_right_table_on_innerjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Join(ActDbContext.TestEntities, p => p.Id, c => c.ParentId, (p, c) => c)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") } });
   }

   [Fact]
   public void Should_delete_projected_right_table_on_leftjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .LeftJoin(ActDbContext.TestEntities, p => p.Id, c => c.ParentId, r => r.Right)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") } });
   }

   [Fact]
   public void Should_delete_projected_left_table_on_innerjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Join(ActDbContext.TestEntities, c => c.ParentId, p => p.Id, (c, p) => c)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") } });
   }

   [Fact]
   public void Should_delete_projected_left_table_on_leftjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .LeftJoin(ActDbContext.TestEntities, c => c.ParentId, p => p.Id, r => r.Left)
                                     .BulkDelete();
      affectedRows.Should().Be(2);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEmpty();
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
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") } });
   }

   [Fact]
   public void Should_delete_1_entity_only()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities.Take(1).BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_throw_if_skip_is_present()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0") });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E") });
      ArrangeDbContext.SaveChanges();

      ActDbContext.TestEntities.Skip(1).Invoking(q => q.BulkDelete())
                  .Should().Throw<NotSupportedException>().WithMessage("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().HaveCount(2);
   }
}