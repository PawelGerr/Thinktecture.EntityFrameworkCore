using Thinktecture.EntityFrameworkCore.Testing;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.QueryableExtensionsTests;

// ReSharper disable InconsistentNaming
public class BulkDeleteAsync : IntegrationTestsBase
{
   public BulkDeleteAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, ITestIsolationOptions.SharedTablesAmbientTransaction)
   {
   }

   [Fact]
   public async Task Should_delete_all_entities()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.BulkDeleteAsync();
      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_ignore_orderby()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.OrderBy(e => e.Id).BulkDeleteAsync();
      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_delete_projected_entities()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities
                                           .SelectMany(e => e.Children)
                                           .BulkDeleteAsync();
      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public async Task Should_delete_entities_matching_where_clause()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), Name = "Test", RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities
                                           .Where(e => e.Name == "Test")
                                           .BulkDeleteAsync();
      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public async Task Should_delete_1_entity_only()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities.Take(1).BulkDeleteAsync();
      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(1);
   }

   [Fact]
   public async Task Should_throw_if_skip_is_present()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TestEntities.Skip(1).Awaiting(q => q.BulkDeleteAsync())
                        .Should().ThrowAsync<NotSupportedException>("An OFFSET clause (i.e. Skip(x)) is not supported in a DELETE statement.");

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(2);
   }

   [Fact]
   public async Task Should_handle_Include_properly()
   {
      var parent = new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" };
      var child = new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName", Parent = parent };
      ArrangeDbContext.Add(parent);
      ArrangeDbContext.Add(child);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities
                                           .Include(e => e.Children)
                                           .Where(e => e.Parent!.Count == 0)
                                           .BulkDeleteAsync();

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" } });
   }

   [Fact]
   public async Task Should_handle_entities_with_inlined_owned_types()
   {
      var entity1 = new TestEntity_Owns_Inline { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), InlineEntity = new OwnedEntity { IntColumn = 1 } };
      var entity2 = new TestEntity_Owns_Inline { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), InlineEntity = new OwnedEntity { IntColumn = 2 } };
      ArrangeDbContext.Add(entity1);
      ArrangeDbContext.Add(entity2);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities_Own_Inline
                                           .Where(e => e.InlineEntity.IntColumn == 1)
                                           .BulkDeleteAsync();

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities_Own_Inline.ToListAsync();
      loadedEntities.Should().BeEquivalentTo(new[] { new TestEntity_Owns_Inline { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), InlineEntity = new OwnedEntity { IntColumn = 2 } } });
   }

   [Fact]
   public async Task Should_throw_if_entity_has_separate_owned_types()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), RequiredName = "RequiredName" });
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.TestEntities_Own_SeparateOne
                        .Awaiting(q => q.BulkDeleteAsync())
                        .Should().ThrowAsync<NotSupportedException>().WithMessage("The provided query is referencing more than 1 table. If the entity has owned types, then please provide just one column of the table to DELETE from [example: Select(x => x.Id).BulkDeleteAsync()]. Found tables: [TestEntities_Own_SeparateOne AS t, SeparateEntitiesOne AS s].");

      var loadedEntities = await AssertDbContext.TestEntities.ToListAsync();
      loadedEntities.Should().HaveCount(2);
   }

   [Fact]
   public async Task Should_delete_entity_with_separate_owned_types_if_column_of_main_entity_is_provided()
   {
      ArrangeDbContext.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("6C410EFE-2A40-4348-8BD6-8E9B9B72F0D0"), SeparateEntity = new OwnedEntity { IntColumn = 1 } });
      ArrangeDbContext.Add(new TestEntity_Owns_SeparateOne { Id = new Guid("C004AB82-803E-4A90-B254-6032B9BBB70E"), SeparateEntity = new OwnedEntity { IntColumn = 2 } });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntities_Own_SeparateOne
                                           .Where(e => e.SeparateEntity!.IntColumn == 1)
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
                                           .Where(e => e.SeparateEntity!.IntColumn == 1)
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
