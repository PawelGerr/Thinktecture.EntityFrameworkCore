using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.Infrastructure;

public class DefaultSchemaTests : IntegrationTestsBase
{
   public DefaultSchemaTests(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public void Should_set_schema_on_context()
   {
      ActDbContext.Schema.Should().Be(Schema);
   }

   [Fact]
   public void Should_set_default_schema_on_model()
   {
      ActDbContext.Model.GetDefaultSchema().Should().Be(Schema);
   }

   [Fact]
   public async Task Should_create_tables_in_configured_schema()
   {
      var tables = await ActDbContext.Database
         .SqlQueryRaw<string>($"""SELECT tablename AS "Value" FROM pg_tables WHERE schemaname = '{Schema}'""")
         .ToListAsync();

      tables.Should().Contain("TestEntities");
   }

   [Fact]
   public async Task Should_insert_and_query_entity_in_configured_schema()
   {
      var id = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = id, RequiredName = "SchemaTest" });
      await ArrangeDbContext.SaveChangesAsync();

      var entity = await AssertDbContext.TestEntities.SingleOrDefaultAsync(e => e.Id == id);

      entity.Should().NotBeNull();
      entity.RequiredName.Should().Be("SchemaTest");
   }
}
