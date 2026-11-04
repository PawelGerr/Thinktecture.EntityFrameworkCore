using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkUpdateFromQueryWithComplexTypesAsync : SchemaChangingIntegrationTestsBase
{
   private SqliteBulkOperationExecutor SUT => field ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkUpdateFromQueryWithComplexTypesAsync(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
   }

   [Fact]
   public async Task Should_update_whole_nested_struct_ComplexType()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ConfigureModel = TestEntityWithNestedStructComplexType.Configure;
      Configure = builder => builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{TestEntityWithNestedStructComplexType.TableName}" ("id" TEXT NOT NULL PRIMARY KEY, "sequence" INTEGER NOT NULL, "received_at" INTEGER NOT NULL, "message_timestamp" INTEGER NOT NULL)
         """);

      ArrangeDbContext.Add(new TestEntityWithNestedStructComplexType { Id = id, Envelope = new MessageEnvelope { Key = new TimestampPair(1, 2), Sequence = 3 } });
      await ArrangeDbContext.SaveChangesAsync();

      var source = new TestEntityWithNestedStructComplexType { Id = id, Envelope = new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 } };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithNestedStructComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => e.Id,
         s => s.Id,
         builder => builder.Set(e => e.Envelope, (e, s) => s.Envelope));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.Set<TestEntityWithNestedStructComplexType>().SingleAsync();
      loaded.Envelope.Should().Be(new MessageEnvelope { Key = new TimestampPair(100, 200), Sequence = 300 });
   }

   [Fact]
   public async Task Should_update_whole_ComplexType_with_value_converted_members_from_captured_variable()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      ConfigureModel = builder =>
                       {
                          TestEntityWithConvertedComplexType.Configure(builder);
                          TempTableWithConvertedComplexType.Configure(builder);
                       };
      Configure = builder => builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

      // The SQLite context has no DbSet for this entity, so EF uses the CLR type name as the table name.
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntityWithConvertedComplexType" ("id" TEXT NOT NULL, "message_timestamp" INTEGER NOT NULL, "effective_received_at" INTEGER NOT NULL, "effective_message_timestamp" INTEGER NOT NULL, PRIMARY KEY ("id", "message_timestamp"))
         """);

      ArrangeDbContext.Set<TestEntityWithConvertedComplexType>().AddRange(
         new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2)) },
         new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) });
      await ArrangeDbContext.SaveChangesAsync();

      var source = new TempTableWithConvertedComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(100), new TimestampValue(200)) };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([source], new SqliteTempTableBulkInsertOptions());

      var value = new ConvertedTimestampPair(new TimestampValue(7), new TimestampValue(8));

      var affectedRows = await ActDbContext.Set<TestEntityWithConvertedComplexType>().BulkUpdateAsync(
         tempTable.Query,
         e => new { e.Id, e.MessageTimestamp },
         s => new { s.Id, s.MessageTimestamp },
         builder => builder.Set(e => e.EffectiveMessage, (e, s) => value));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.Set<TestEntityWithConvertedComplexType>().OrderBy(e => e.MessageTimestamp).ToListAsync();
      loaded.Should().BeEquivalentTo(
      [
         new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 10, EffectiveMessage = value },
         new TestEntityWithConvertedComplexType { Id = id, MessageTimestamp = 11, EffectiveMessage = new ConvertedTimestampPair(new TimestampValue(3), new TimestampValue(4)) }
      ]);
   }
}
