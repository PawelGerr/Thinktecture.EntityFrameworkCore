using Thinktecture.TestDatabaseContext;

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
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities.BulkDelete();
      affectedRows.Should().Be(2);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public void Should_ignore_orderby()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities.OrderBy(e => e.Id).BulkDelete();
      affectedRows.Should().Be(2);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public void Should_delete_projected_entities()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .SelectMany(e => e.Children)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public void Should_ignore_included_entities()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Include(e => e.Children)
                                     .Where(i => i.ParentId != null)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public void Should_throw_on_innerjoin_projecting_multiple_tables()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
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
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
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
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Join(ActDbContext.TestEntities, p => p.Id, c => c.ParentId, (p, c) => c)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public void Should_delete_projected_right_table_on_leftjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .LeftJoin(ActDbContext.TestEntities, p => p.Id, c => c.ParentId, r => r.Right)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public void Should_delete_projected_left_table_on_innerjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Join(ActDbContext.TestEntities, c => c.ParentId, p => p.Id, (c, p) => c)
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public void Should_delete_projected_left_table_on_leftjoin()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
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
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), Name = "Test", RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Where(e => e.Name == "Test")
                                     .BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public void Should_delete_1_entity_only()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities.Take(1).BulkDelete();
      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().HaveCount(1);
   }

   [Fact]
   public void Should_throw_if_skip_is_present()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      ArrangeDbContext.SaveChanges();

      ActDbContext.TestEntities.Skip(1).Invoking(q => q.BulkDelete())
                  .Should().Throw<NotSupportedException>().WithMessage("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().HaveCount(2);
   }

   [Fact]
   public void Should_handle_Include_properly()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      ArrangeDbContext.SaveChanges();

      var affectedRows = ActDbContext.TestEntities
                                     .Include(e => e.Children)
                                     .Where(e => e.Parent!.Count == 0)
                                     .BulkDelete();

      affectedRows.Should().Be(1);

      var loadedEntities = AssertDbContext.TestEntities.ToList();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public async Task Should_delete_entity_with_separate_owned_types_if_column_of_main_entity_is_provided()
   {
      ArrangeDbContext.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), SeparateEntity = new OwnedEntity { IntColumn = 1 } });
      ArrangeDbContext.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), SeparateEntity = new OwnedEntity { IntColumn = 2 } });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities_Own_SeparateOne
                                           .Where(e => e.SeparateEntity.IntColumn == 1)
                                           .Select(e => e.Id)
                                           .BulkDeleteAsync();

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity_Owns_SeparateOne { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), SeparateEntity = new OwnedEntity { IntColumn = 2 } } });
   }

   [Fact]
   public async Task Should_delete_separate_owned_entity_only_if_column_of_owned_entity_is_provided()
   {
      ArrangeDbContext.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), SeparateEntity = new OwnedEntity { IntColumn = 1 } });
      ArrangeDbContext.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), SeparateEntity = new OwnedEntity { IntColumn = 2 } });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities_Own_SeparateOne
                                           .Where(e => e.SeparateEntity.IntColumn == 1)
                                           .Select(e => e.SeparateEntity)
                                           .BulkDeleteAsync();

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities_Own_SeparateOne.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[]
                                             {
                                                new TestEntity_Owns_SeparateOne { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), SeparateEntity = null! },
                                                new TestEntity_Owns_SeparateOne { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), SeparateEntity = new OwnedEntity { IntColumn = 2 } }
                                             });
   }
}
