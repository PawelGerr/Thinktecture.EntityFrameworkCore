using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.RelationalPropertyBuilderExtensionsTests;

public class UseReferenceEqualityComparer : IntegrationTestsBase
{
   public UseReferenceEqualityComparer(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, MigrationExecutionStrategies.EnsureCreated)
   {
   }

   [Fact]
   public async Task Should_not_flag_entity_as_changed_if_property_unchanged()
   {
      ArrangeDbContext.EntitiesWithArrayValueComparer
                      .Add(new EntityWithArrayValueComparer(new Guid("58AF51AD-2CB0-491D-88D2-64CAAC6A05B1"), new byte[] { 1, 2, 3 }));
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.EntitiesWithArrayValueComparer.SingleAsync();

      // no changes
      var affectedRows = await ActDbContext.SaveChangesAsync();

      affectedRows.Should().Be(0);

      var loadedEntity = await AssertDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      loadedEntity.Bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
   }

   [Fact]
   public async Task Should_not_flag_entity_as_changed_if_reference_unchanged_but_content_changed()
   {
      ArrangeDbContext.EntitiesWithArrayValueComparer
                      .Add(new EntityWithArrayValueComparer(new Guid("58AF51AD-2CB0-491D-88D2-64CAAC6A05B1"), new byte[] { 1, 2, 3 }));
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      entity.Bytes[2] = 42;

      var affectedRows = await ActDbContext.SaveChangesAsync();

      affectedRows.Should().Be(0);

      var loadedEntity = await AssertDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      loadedEntity.Bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
   }

   [Fact]
   public async Task Should_flag_entity_as_modified_if_reference_changed_but_content_unchanged()
   {
      ArrangeDbContext.EntitiesWithArrayValueComparer
                      .Add(new EntityWithArrayValueComparer(new Guid("58AF51AD-2CB0-491D-88D2-64CAAC6A05B1"), new byte[] { 1, 2, 3 }));
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      entity.Bytes = new byte[] { 1, 2, 3 };

      var affectedRows = await ActDbContext.SaveChangesAsync();

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      loadedEntity.Bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
   }

   [Fact]
   public async Task Should_flag_entity_as_modified_if_reference_changed_and_content_changed()
   {
      ArrangeDbContext.EntitiesWithArrayValueComparer
                      .Add(new EntityWithArrayValueComparer(new Guid("58AF51AD-2CB0-491D-88D2-64CAAC6A05B1"), new byte[] { 1, 2, 3 }));
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await ActDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      entity.Bytes = new byte[] { 1, 2, 42 };

      var affectedRows = await ActDbContext.SaveChangesAsync();

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.EntitiesWithArrayValueComparer.SingleAsync();
      loadedEntity.Bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 42 });
   }
}
