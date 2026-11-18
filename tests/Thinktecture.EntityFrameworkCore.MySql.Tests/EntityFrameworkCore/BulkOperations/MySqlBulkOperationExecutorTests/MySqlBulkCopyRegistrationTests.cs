using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// "AddBulkOperationSupport(useBulkCopy: true)" makes every insert of the executor use MySqlBulkCopy (LOAD DATA LOCAL INFILE),
// also the fill of the staging tables of bulk update and upsert. Com_load counts the LOAD DATA statements of the session, Com_insert the INSERT statements.
[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkCopyRegistrationTests : MySqlIntegrationTestsBase
{
   public MySqlBulkCopyRegistrationTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
      UseBulkCopy = true;
   }

   [Fact]
   public async Task Should_bulk_insert_with_load_data()
   {
      await using var context = await CreateOpenContextAsync();
      var counters = await StatementCounters.ReadAsync(context);

      var affectedRows = await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }, new MySqlTestEntity { Id = 2, Name = null }]);

      affectedRows.Should().Be(2);
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 1, Inserts: 0));
      (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A", null);
   }

   [Fact]
   public async Task Should_insert_owned_types_in_separate_tables_with_load_data()
   {
      await using var context = await CreateOpenContextAsync();
      var owner = new MySqlOwnerEntity
                  {
                     Id = 1,
                     Address = new MySqlAddress { City = "Berlin" },
                     Phones = [new MySqlPhone { Number = "1" }, new MySqlPhone { Number = "2" }]
                  };

      // Owned entities with their own table must be tracked.
      context.Add(owner);
      var counters = await StatementCounters.ReadAsync(context);

      var affectedRows = await context.BulkInsertAsync([owner]);

      affectedRows.Should().Be(4, "1 owner, 1 address and 2 phones");
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 3, Inserts: 0));

      context.ChangeTracker.Clear();
      var loaded = await context.Owners.AsNoTracking().SingleAsync();
      loaded.Address!.City.Should().Be("Berlin");
      loaded.Phones.Select(phone => phone.Number).Should().BeEquivalentTo("1", "2");
   }

   [Fact]
   public async Task Should_bulk_insert_into_temp_table_with_load_data()
   {
      await using var context = await CreateOpenContextAsync();
      var counters = await StatementCounters.ReadAsync(context);

      await using var tempTable = await context.BulkInsertIntoTempTableAsync([new MySqlTestTempEntity { Id = 1, Name = "A" }, new MySqlTestTempEntity { Id = 2, Name = "B" }]);

      tempTable.NumberOfInsertedRows.Should().Be(2);
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 1, Inserts: 0));
      (await tempTable.Query.OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A", "B");
   }

   [Fact]
   public async Task Should_bulk_insert_values_into_temp_table_with_load_data()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<int>();
      await using var context = await CreateOpenContextAsync();
      var counters = await StatementCounters.ReadAsync(context);

      await using var tempTable = await context.BulkInsertValuesIntoTempTableAsync([3, 1, 2]);

      tempTable.NumberOfInsertedRows.Should().Be(3);
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 1, Inserts: 0));
      (await tempTable.Query.OrderBy(value => value).ToListAsync()).Should().Equal(1, 2, 3);
   }

   [Fact]
   public async Task Should_fill_the_staging_table_of_bulk_update_with_load_data()
   {
      await using var context = await CreateOpenContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }, new MySqlTestEntity { Id = 2, Name = "B" }]);
      var counters = await StatementCounters.ReadAsync(context);

      var affectedRows = await context.BulkUpdateAsync([new MySqlTestEntity { Id = 1, Name = "A changed" }, new MySqlTestEntity { Id = 2, Name = null }], entity => entity.Name);

      affectedRows.Should().Be(2);
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 1, Inserts: 0));
      (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A changed", null);
   }

   [Fact]
   public async Task Should_fill_the_staging_table_of_upsert_with_load_data()
   {
      await using var context = await CreateOpenContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }]);
      var counters = await StatementCounters.ReadAsync(context);

      var affectedRows = await context.BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "A changed" }, new MySqlTestEntity { Id = 2, Name = "B" }]);

      affectedRows.Should().Be(2);

      // The new rows are copied from the staging table with INSERT ... SELECT, which is counted in Com_insert_select.
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 1, Inserts: 0));
      (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A changed", "B");
   }

   [Fact]
   public async Task Should_update_typed_values_through_a_staging_table_filled_by_load_data()
   {
      await using var context = await CreateOpenContextAsync();
      var id = Guid.NewGuid();
      await context.BulkInsertAsync([new MySqlTypedEntity { Id = id, Amount = 1m, Payload = "{\"a\":1}", CreatedAt = new DateTime(2020, 1, 1), Status = MySqlTestStatus.Draft, Data = [1], Flag = false }]);
      var entity = new MySqlTypedEntity
                   {
                      Id = id,
                      Amount = 1234.5678m,
                      Payload = null,
                      CreatedAt = new DateTime(2021, 2, 3, 4, 5, 6, DateTimeKind.Unspecified).AddTicks(1_234_560),
                      Status = MySqlTestStatus.Published,
                      Data = [1, 2, 255],
                      Flag = true
                   };
      var counters = await StatementCounters.ReadAsync(context);

      var affectedRows = await context.BulkUpdateAsync([entity]);

      affectedRows.Should().Be(1);
      (await counters.GetDeltaAsync(context)).Should().Be(new StatementCounters(Loads: 1, Inserts: 0));
      context.ChangeTracker.Clear();
      (await context.TypedEntities.AsNoTracking().SingleAsync()).Should().BeEquivalentTo(entity);
   }

   [Fact]
   public async Task Should_throw_if_the_connection_does_not_allow_load_local_infile()
   {
      await using var context = await CreateContextAsync();
      context.Database.SetConnectionString(new MySqlConnectionStringBuilder(ConnectionString) { AllowLoadLocalInfile = false }.ConnectionString);

      var act = () => context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }]);

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*useBulkCopy*AllowLoadLocalInfile=true*");
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_apply_the_default_command_timeout_of_the_connection_to_bulk_copy()
   {
      // The uncommitted row of another transaction blocks the insert of the same primary key
      // until the lock wait timeout of the server (50 seconds by default) is reached.
      await using var blockingContext = await CreateContextAsync();
      await using var blockingTransaction = await blockingContext.Database.BeginTransactionAsync();
      blockingContext.Entities.Add(new MySqlTestEntity { Id = 1, Name = "Blocking" });
      await blockingContext.SaveChangesAsync();

      // Neither the options nor the context have a timeout, so the default of the connection applies (30 seconds without this setting).
      await using var context = await CreateContextAsync();
      context.Database.SetConnectionString(new MySqlConnectionStringBuilder(ConnectionString) { AllowLoadLocalInfile = true, DefaultCommandTimeout = 1 }.ConnectionString);
      var stopwatch = Stopwatch.StartNew();

      await context.Awaiting(ctx => ctx.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Blocked" }]))
                   .Should().ThrowAsync<MySqlException>();

      stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
   }

   [Fact]
   public async Task Should_throw_for_a_year_column()
   {
      // MySqlBulkCopy stores 2000 for the value 0 of a YEAR column, whereas INSERT stores 0000.
      ConfigureModel = builder => builder.ConfigureTempTable<short>(typeBuilder => typeBuilder.Property(t => t.Column1).HasColumnType("year"));
      await using var context = await CreateContextAsync();

      var act = () => context.BulkInsertValuesIntoTempTableAsync(new short[] { 0, 2024 });

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*year*useBulkCopy*");
      context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_insert_with_load_data_inside_an_xa_transaction_scope(bool completeScope)
   {
      await using var context = await CreateContextAsync();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         (await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Scoped" }])).Should().Be(1);

         if (completeScope)
            scope.Complete();
      }

      (await context.Entities.CountAsync()).Should().Be(completeScope ? 1 : 0);
   }

   [Fact]
   public async Task Should_throw_and_not_leave_rows_if_a_duplicate_key_is_copied()
   {
      await using var context = await CreateContextAsync();

      var act = () => context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "First" }, new MySqlTestEntity { Id = 1, Name = "Duplicate" }]);

      // MySqlBulkCopy compares the copied and the inserted rows itself; the own transaction of the executor removes the first row.
      await act.Should().ThrowAsync<MySqlException>().WithMessage("*only 1 was inserted*");
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_and_not_leave_rows_if_a_string_is_too_long()
   {
      await using var context = await CreateContextAsync();

      var act = () => context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Valid" }, new MySqlTestEntity { Id = 2, Name = new string('x', 300) }]);

      // LOAD DATA LOCAL implies IGNORE, i.e. MySQL truncates the value and reports a warning, which the executor turns into an exception.
      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("MySqlBulkCopy inserted*");
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(3)]
   public async Task Should_rethrow_the_exception_of_the_source_and_not_leave_rows(int rowsBeforeFailure)
   {
      await using var context = await CreateContextAsync();

      var act = () => context.BulkInsertAsync(EntitiesThenThrow(rowsBeforeFailure));

      // The caller gets the exception of the source itself, not a MySqlException of the aborted LOAD DATA.
      await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Source failed.");
      context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
      (await context.Entities.CountAsync()).Should().Be(0);

      // The context stays usable.
      (await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "After" }])).Should().Be(1);
   }

   [Fact]
   public async Task Should_rethrow_the_exception_of_the_source_when_inserting_into_a_temp_table()
   {
      await using var context = await CreateContextAsync();

      var act = () => context.BulkInsertIntoTempTableAsync(TempEntitiesThenThrow());

      // The temp table reference is disposed after the failure, so it drops the table and closes the connection that it opened.
      await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Source failed.");
      context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);

      // The context stays usable.
      await using var tempTable = await context.BulkInsertIntoTempTableAsync([new MySqlTestTempEntity { Id = 1, Name = "After" }]);
      (await tempTable.Query.Select(entity => entity.Name).ToListAsync()).Should().Equal("After");

      static IEnumerable<MySqlTestTempEntity> TempEntitiesThenThrow()
      {
         yield return new MySqlTestTempEntity { Id = 1, Name = "Name 1" };

         throw new InvalidOperationException("Source failed.");
      }
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_keep_the_connection_and_the_transaction_of_the_caller_if_the_source_throws(bool commit)
   {
      await using var context = await CreateContextAsync();
      await using var transaction = await context.Database.BeginTransactionAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 100, Name = "Earlier" }]);

      var act = () => context.BulkInsertAsync(EntitiesThenThrow(3));

      await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Source failed.");
      context.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
      context.Database.CurrentTransaction.Should().NotBeNull();

      // The earlier row and the three rows before the failing row are part of the transaction.
      (await context.Entities.CountAsync()).Should().Be(4);
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 101, Name = "Later" }]);

      if (commit)
         await transaction.CommitAsync();
      else
         await transaction.RollbackAsync();

      var ids = await AssertDbContext.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Id).ToListAsync();
      ids.Should().Equal(commit ? [1, 2, 3, 100, 101] : []);
   }

   [Fact]
   public async Task Should_keep_the_transaction_of_the_caller_if_the_source_throws_during_the_staging_fill()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Old 1" }, new MySqlTestEntity { Id = 2, Name = "Old 2" }]);
      var executor = context.GetService<MySqlBulkOperationExecutor>();
      await using var transaction = await context.Database.BeginTransactionAsync();

      var act = () => executor.BulkUpdateAsync(EntitiesThenThrow(1), new MySqlBulkUpdateOptions());

      await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Source failed.");
      context.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);

      // The staging table is dropped and its name is free, so the same operation works in the same transaction.
      (await executor.BulkUpdateAsync([new MySqlTestEntity { Id = 2, Name = "New 2" }], new MySqlBulkUpdateOptions())).Should().Be(1);
      await transaction.CommitAsync();

      (await AssertDbContext.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("Old 1", "New 2");
   }

   [Fact]
   public async Task Should_rethrow_the_exception_of_a_value_converter_and_not_leave_rows()
   {
      ConfigureModel = builder => builder.Entity<MySqlTestEntity>().Property(entity => entity.Name).HasConversion(name => FailForBadName(name), name => name);
      await using var context = await CreateContextAsync();

      var act = () => context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Good" }, new MySqlTestEntity { Id = 2, Name = "Bad" }]);

      await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Converter failed.");
      context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   // The status counters are per session, so the connection stays open and the executor and the status queries share the session.
   private async Task<MySqlTestDbContext> CreateOpenContextAsync()
   {
      var context = await CreateContextAsync();
      await context.Database.OpenConnectionAsync();
      return context;
   }

   private static IEnumerable<MySqlTestEntity> EntitiesThenThrow(int count)
   {
      for (var id = 1; id <= count; id++)
      {
         yield return new MySqlTestEntity { Id = id, Name = $"Name {id}" };
      }

      throw new InvalidOperationException("Source failed.");
   }

   private static string? FailForBadName(string? name)
   {
      return name == "Bad" ? throw new InvalidOperationException("Converter failed.") : name;
   }

   private sealed record StatementCounters(long Loads, long Inserts)
   {
      public static async Task<StatementCounters> ReadAsync(DbContext context)
      {
         return new StatementCounters(await GetSessionStatusAsync(context, "Com_load"), await GetSessionStatusAsync(context, "Com_insert"));
      }

      public async Task<StatementCounters> GetDeltaAsync(DbContext context)
      {
         var current = await ReadAsync(context);
         return new StatementCounters(current.Loads - Loads, current.Inserts - Inserts);
      }

      private static async Task<long> GetSessionStatusAsync(DbContext context, string name)
      {
         await using var command = context.Database.GetDbConnection().CreateCommand();
         command.CommandText = $"SHOW SESSION STATUS LIKE '{name}'";
         await using var reader = await command.ExecuteReaderAsync();
         await reader.ReadAsync();

         return Int64.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
      }
   }
}
