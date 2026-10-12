using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateWithOwnedTypesAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor? _sut;
   private SqliteBulkOperationExecutor SUT => _sut ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkUpdateWithOwnedTypesAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
      ConfigureModel = TestEntity_Owns_TwoSeparateOnes.Configure;
      Configure = builder => builder.ConfigureWarnings(warningsBuilder => warningsBuilder.Ignore(RelationalEventId.PendingModelChangesWarning));
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_update_root_and_owned_entities_in_tables_from_table_name_provider(bool useGenericOptions)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_Own_TwoSeparateOnes_Redirect" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NULL);
         CREATE TABLE "TwoSeparateOnes_First_Redirect" ("TestEntity_Owns_TwoSeparateOnesId" TEXT NOT NULL PRIMARY KEY, "StringColumn" TEXT NULL, "IntColumn" INTEGER NOT NULL);
         CREATE TABLE "TwoSeparateOnes_Second_Redirect" ("TestEntity_Owns_TwoSeparateOnesId" TEXT NOT NULL PRIMARY KEY, "StringColumn" TEXT NULL, "IntColumn" INTEGER NOT NULL);
         INSERT INTO "TestEntities_Own_TwoSeparateOnes_Redirect" ("Id", "Name") VALUES ('7C00ABFE-875B-4396-BE51-3E898647A264', 'old');
         INSERT INTO "TwoSeparateOnes_First_Redirect" ("TestEntity_Owns_TwoSeparateOnesId", "StringColumn", "IntColumn") VALUES ('7C00ABFE-875B-4396-BE51-3E898647A264', NULL, 0);
         INSERT INTO "TwoSeparateOnes_Second_Redirect" ("TestEntity_Owns_TwoSeparateOnesId", "StringColumn", "IntColumn") VALUES ('7C00ABFE-875B-4396-BE51-3E898647A264', NULL, 0);
         """);

      var testEntity = new TestEntity_Owns_TwoSeparateOnes
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          Name = "new",
                          First = new OwnedEntity { IntColumn = 1 },
                          Second = new OwnedEntity { IntColumn = 2 }
                       };
      ActDbContext.Set<TestEntity_Owns_TwoSeparateOnes>().Add(testEntity);
      var provider = new SuffixTableNameProvider("_Redirect");

      IBulkUpdateOptions options = useGenericOptions
                                      ? new GenericBulkOperationOptions { TableNameProvider = provider }
                                      : new SqliteBulkUpdateOptions { TableNameProvider = provider };

      var affectedRows = await SUT.BulkUpdateAsync([testEntity], options);

      affectedRows.Should().Be(3);
      (await AssertDbContext.Database.SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_Own_TwoSeparateOnes_Redirect" """).ToListAsync()).Should().BeEquivalentTo(["new"]);
      (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "TwoSeparateOnes_First_Redirect" """).ToListAsync()).Should().BeEquivalentTo([1]);
      (await AssertDbContext.Database.SqlQueryRaw<int>("""SELECT "IntColumn" FROM "TwoSeparateOnes_Second_Redirect" """).ToListAsync()).Should().BeEquivalentTo([2]);
      provider.RequestedEntityTypes.Should().HaveCount(3);
   }

   [Fact]
   public async Task Should_throw_when_table_name_is_overridden_and_owned_entities_have_separate_tables()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""CREATE TABLE "TestEntities_Own_TwoSeparateOnes_Redirect" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NULL);""");

      var testEntity = new TestEntity_Owns_TwoSeparateOnes
                       {
                          Id = new Guid("7C00ABFE-875B-4396-BE51-3E898647A264"),
                          Name = "new",
                          First = new OwnedEntity { IntColumn = 1 },
                          Second = new OwnedEntity { IntColumn = 2 }
                       };
      ActDbContext.Set<TestEntity_Owns_TwoSeparateOnes>().Add(testEntity);

      await SUT.Awaiting(s => s.BulkUpdateAsync([testEntity], new SqliteBulkUpdateOptions { TableName = "TestEntities_Own_TwoSeparateOnes_Redirect" }))
               .Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*'TableName'*'TableNameProvider'*");
   }
}
