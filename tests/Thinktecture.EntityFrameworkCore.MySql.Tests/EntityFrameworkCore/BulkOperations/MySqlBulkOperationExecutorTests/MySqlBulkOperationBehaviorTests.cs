using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkOperationBehaviorTests : MySqlIntegrationTestsBase
{
   public MySqlBulkOperationBehaviorTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_round_trip_typed_values_through_bulk_insert()
   {
      await using var context = await CreateContextAsync();
      var id = Guid.NewGuid();
      var createdAt = new DateTime(2020, 1, 2, 3, 4, 5, 123, DateTimeKind.Unspecified).AddTicks(4560);

      await context.BulkInsertAsync([
         new MySqlTypedEntity
         {
            Id = id,
            Amount = 1234.5678m,
            Payload = "{\"a\":1}",
            CreatedAt = createdAt,
            Status = MySqlTestStatus.Published,
            Data = [1, 2, 3],
            Flag = true
         }
      ]);

      context.ChangeTracker.Clear();
      var loaded = await context.TypedEntities.AsNoTracking().SingleAsync();
      loaded.Id.Should().Be(id);
      loaded.Amount.Should().Be(1234.5678m);
      loaded.Payload.Should().Contain("\"a\"");
      loaded.CreatedAt.Should().Be(createdAt);
      loaded.Status.Should().Be(MySqlTestStatus.Published);
      loaded.Data.Should().Equal(1, 2, 3);
      loaded.Flag.Should().BeTrue();
   }

   // MySQL rounds a decimal with more decimals than the scale of the column and reports Note 1265; both insert paths accept the value.
   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_round_decimals_with_more_decimals_than_the_column_scale(bool useBulkCopy)
   {
      UseBulkCopy = useBulkCopy;
      await using var context = await CreateContextAsync();

      var affectedRows = await context.BulkInsertAsync([new MySqlTypedEntity { Id = Guid.NewGuid(), Amount = 1.23456m, CreatedAt = new DateTime(2020, 1, 2) }]);

      affectedRows.Should().Be(1);
      (await context.TypedEntities.AsNoTracking().Select(entity => entity.Amount).SingleAsync()).Should().Be(1.2346m);
   }

   [Fact]
   public async Task Should_round_trip_typed_values_over_several_prepared_batches()
   {
      await using var context = await CreateContextAsync();

      // MySqlTypedEntity has 7 columns, so 2 rows fit into one statement: 3 full batches run the prepared statement, the last row runs alone.
      // The NULL values alternate between the rows, so a parameter of the prepared statement changes between NULL and a value.
      var entities = Enumerable.Range(0, 7)
                               .Select(i => new MySqlTypedEntity
                                            {
                                               Id = Guid.NewGuid(),
                                               Amount = 1000.1234m + i,
                                               Payload = i % 2 == 0 ? $"{{\"a\":{i}}}" : null,
                                               CreatedAt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Unspecified).AddTicks(i * 10 + 1_234_560),
                                               Status = i % 2 == 0 ? MySqlTestStatus.Published : MySqlTestStatus.Draft,
                                               Data = i % 2 == 1 ? [(byte)i, 2, 3] : null,
                                               Flag = i % 3 == 0
                                            })
                               .ToList();

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(entities, new MySqlBulkInsertOptions { MaxParametersPerStatement = 14 });

      affectedRows.Should().Be(7);
      context.ChangeTracker.Clear();
      var loaded = await context.TypedEntities.AsNoTracking().ToListAsync();
      loaded.Should().BeEquivalentTo(entities, config => config.Excluding(entity => entity.Payload));

      foreach (var entity in entities)
      {
         var payload = loaded.Single(e => e.Id == entity.Id).Payload;

         if (entity.Payload is null)
            payload.Should().BeNull();
         else
            payload!.Replace(" ", String.Empty, StringComparison.Ordinal).Should().Be(entity.Payload);
      }
   }

   [Fact]
   public async Task Should_insert_with_mysql_bulk_copy_when_registered()
   {
      UseBulkCopy = true;
      await using var context = await CreateContextAsync();
      await context.Database.OpenConnectionAsync();

      try
      {
         var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
            [new MySqlTestEntity { Id = 901, Name = "BulkCopy-A" }, new MySqlTestEntity { Id = 902, Name = "BulkCopy-B" }],
            new MySqlBulkInsertOptions());

         affectedRows.Should().Be(2);
         (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToArrayAsync())
            .Should().Equal("BulkCopy-A", "BulkCopy-B");
      }
      finally
      {
         await context.Database.CloseConnectionAsync();
      }
   }

   [Fact]
   public async Task Should_not_leave_partial_rows_when_a_later_batch_of_an_insert_fails()
   {
      await using var context = await CreateContextAsync();
      var executor = context.GetService<MySqlBulkOperationExecutor>();
      var options = new MySqlBulkInsertOptions { MaxParametersPerStatement = 1 };

      var act = () => executor.BulkInsertAsync(
         [
            new MySqlTestEntity { Id = 1, Name = "A" },
            new MySqlTestEntity { Id = 2, Name = "B" },
            new MySqlTestEntity { Id = 3, Name = new string('x', 300) }
         ],
         options);

      // 1406: data too long for the column (strict mode)
      (await act.Should().ThrowAsync<MySqlConnector.MySqlException>()).Which.Number.Should().Be(1406);
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_leave_partial_rows_when_a_later_batch_of_an_upsert_fails()
   {
      await using var context = await CreateContextAsync();
      var executor = context.GetService<MySqlBulkOperationExecutor>();
      var options = new MySqlBulkInsertOrUpdateOptions { MaxParametersPerStatement = 1 };

      var act = () => executor.BulkInsertOrUpdateAsync(
         [
            new MySqlTestEntity { Id = 1, Name = "A" },
            new MySqlTestEntity { Id = 2, Name = "B" },
            new MySqlTestEntity { Id = 3, Name = new string('x', 300) }
         ],
         options);

      // 1406: data too long for the column (strict mode)
      (await act.Should().ThrowAsync<MySqlConnector.MySqlException>()).Which.Number.Should().Be(1406);
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_modify_the_callers_options_when_inserting_into_an_existing_temp_table()
   {
      await using var context = await CreateContextAsync();
      var executor = context.GetService<MySqlBulkOperationExecutor>();
      var options = new MySqlBulkInsertOptions();
      var tempTable = await context.CreateTempTableAsync<MySqlTestTempEntity>(new TempTableCreationOptions());

      try
      {
         await executor.BulkInsertIntoTempTableAsync([new MySqlTestTempEntity { Id = 1, Name = "Temp" }], tempTable, options, CancellationToken.None);
         options.TableNameProvider.Should().BeSameAs(IBulkOperationTableNameProvider.Default);

         await executor.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Regular" }], options);
         (await context.Entities.CountAsync()).Should().Be(1);
      }
      finally
      {
         await tempTable.DisposeAsync();
      }
   }

   [Fact]
   public async Task Should_insert_regular_entities_into_a_temp_table()
   {
      await using var context = await CreateContextAsync();

      await using var tempTable = await context.BulkInsertIntoTempTableAsync(
         [new MySqlTestEntity { Id = 1, Name = "Regular" }],
         context.GetService<ITempTableBulkInsertExecutor>().CreateOptions());

      (await tempTable.Query.Select(entity => entity.Name).SingleAsync()).Should().Be("Regular");
   }

   [Fact]
   public async Task Should_update_many_rows_with_a_staging_table_in_multiple_batches()
   {
      await using var context = await CreateContextAsync();
      var originals = Enumerable.Range(1, 1000).Select(id => new MySqlTestEntity { Id = id, Name = $"Old-{id}" }).ToArray();
      await context.BulkInsertAsync(originals);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         originals.Select(entity => new MySqlTestEntity { Id = entity.Id, Name = $"New-{entity.Id}" }).ToArray(),
         new MySqlBulkUpdateOptions
         {
            MaxParametersPerStatement = 200,
            PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlTestEntity>(entity => entity.Name)
         });

      affectedRows.Should().Be(1000);
      (await context.Entities.AsNoTracking().CountAsync(entity => entity.Name!.StartsWith("New-"))).Should().Be(1000);
   }

   [Fact]
   public async Task Should_insert_owned_types_stored_in_separate_tables()
   {
      await using var context = await CreateContextAsync();

      // Owned entities with their own table must be tracked.
      var owner = new MySqlOwnerEntity
                  {
                     Id = 1,
                     Address = new MySqlAddress { City = "Berlin" },
                     Phones = [new MySqlPhone { Number = "1" }, new MySqlPhone { Number = "2" }]
                  };
      context.Add(owner);

      await context.BulkInsertAsync([owner]);

      context.ChangeTracker.Clear();
      var loaded = await context.Owners.AsNoTracking().SingleAsync();
      loaded.Address!.City.Should().Be("Berlin");
      loaded.Phones.Select(phone => phone.Number).Should().BeEquivalentTo("1", "2");
   }

   [Fact]
   public async Task Should_match_string_keys_with_the_collation_of_the_model()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([
         new MySqlCollatedEntity { Id = 1, Name = "Aa" },
         new MySqlCollatedEntity { Id = 2, Name = "aa" }
      ]);

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         [new MySqlCollatedEntity { Name = "Aa", Count = 5 }],
         new MySqlBulkUpdateOptions
         {
            PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlCollatedEntity>(entity => entity.Count),
            KeyProperties = IEntityPropertiesProvider.Include<MySqlCollatedEntity>(entity => entity.Name)
         });

      affectedRows.Should().Be(1);
      (await context.CollatedEntities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Count).ToArrayAsync()).Should().Equal(5, 0);
   }

   // ExecuteSqlRawAsync and BeginTransactionAsync do not use the execution strategy, so a retrying strategy does not reject the own transaction of the executor.
   [Fact]
   public async Task Should_bulk_insert_with_a_retrying_execution_strategy()
   {
      await using var context = await CreateContextAsync(builder => builder.EnableRetryOnFailure());

      (await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }, new MySqlTestEntity { Id = 2, Name = "B" }])).Should().Be(2);

      (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A", "B");
   }

   [Fact]
   public async Task Should_bulk_update_with_a_retrying_execution_strategy()
   {
      await using var context = await CreateContextAsync(builder => builder.EnableRetryOnFailure());
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }]);

      (await context.BulkUpdateAsync([new MySqlTestEntity { Id = 1, Name = "A changed" }], entity => entity.Name)).Should().Be(1);

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("A changed");
   }

   [Fact]
   public async Task Should_bulk_upsert_with_a_retrying_execution_strategy()
   {
      await using var context = await CreateContextAsync(builder => builder.EnableRetryOnFailure());
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "A" }]);

      (await context.BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "A changed" }, new MySqlTestEntity { Id = 2, Name = "B" }])).Should().Be(2);

      (await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A changed", "B");
   }

   [Fact]
   public async Task Should_bulk_insert_into_temp_table_and_query_it_with_a_retrying_execution_strategy()
   {
      await using var context = await CreateContextAsync(builder => builder.EnableRetryOnFailure());

      await using var tempTable = await context.BulkInsertIntoTempTableAsync([new MySqlTestTempEntity { Id = 1, Name = "A" }, new MySqlTestTempEntity { Id = 2, Name = "B" }]);

      (await tempTable.Query.OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync()).Should().Equal("A", "B");
   }

   [Fact]
   public async Task Should_dispose_a_temp_table_reference_twice_without_error()
   {
      await using var context = await CreateContextAsync();
      var tempTable = await context.CreateTempTableAsync<MySqlTestTempEntity>(new TempTableCreationOptions());

      await tempTable.DisposeAsync();
      await tempTable.DisposeAsync();
      tempTable.Dispose();

      context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
   }

   [Fact]
   public void Should_reject_window_functions_on_servers_that_do_not_support_them()
   {
      var options = MySqlTestDbContext.CreateOptionsBuilder()
                    .UseMySql("Server=localhost;Database=test", new MySqlServerVersion(new Version(5, 7, 40)), builder => builder.AddWindowFunctionsSupport())
                    .Options;
      var act = () => new MySqlTestDbContext(options).Entities.ToQueryString();

      act.Should().Throw<InvalidOperationException>().WithMessage("*Window functions require*");
   }

   [Fact]
   public void Should_reject_collection_parameters_on_servers_without_json_table()
   {
      var options = MySqlTestDbContext.CreateOptionsBuilder()
                    .UseMySql("Server=localhost;Database=test", new MySqlServerVersion(new Version(8, 0, 3)), builder => builder.AddCollectionParameterSupport())
                    .Options;
      var act = () => new MySqlTestDbContext(options).Entities.ToQueryString();

      act.Should().Throw<InvalidOperationException>().WithMessage("*JSON_TABLE*");
   }
}
