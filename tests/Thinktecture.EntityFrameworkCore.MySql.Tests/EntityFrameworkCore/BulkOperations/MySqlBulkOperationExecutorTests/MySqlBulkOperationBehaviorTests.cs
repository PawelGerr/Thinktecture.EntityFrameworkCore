using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlBulkOperationBehaviorTests : MySqlIntegrationTestsBase
{
   public MySqlBulkOperationBehaviorTests(MySqlContainerFixture fixture)
      : base(fixture)
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

   [Fact]
   public async Task Should_insert_with_mysql_bulk_copy_when_enabled()
   {
      await using var context = await CreateContextAsync();
      var connectionString = new MySqlConnector.MySqlConnectionStringBuilder(context.Database.GetConnectionString()
                                            ?? throw new InvalidOperationException("The MySQL connection string is not configured."))
                            {
                               AllowLoadLocalInfile = true
                            };
      context.Database.SetConnectionString(connectionString.ConnectionString);
      await context.Database.OpenConnectionAsync();

      try
      {
         var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
            [new MySqlTestEntity { Id = 901, Name = "BulkCopy-A" }, new MySqlTestEntity { Id = 902, Name = "BulkCopy-B" }],
            new MySqlBulkInsertOptions { UseBulkCopy = true });

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
      var options = new MySqlBulkInsertOptions { BatchSize = 1 };

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
      var options = new MySqlBulkInsertOrUpdateOptions { BatchSize = 1 };

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
            BatchSize = 100,
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
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", new MySqlServerVersion(new Version(5, 7, 40)), builder => builder.AddWindowFunctionsSupport())
                    .Options;
      var act = () => new MySqlTestDbContext(options).Entities.ToQueryString();

      act.Should().Throw<InvalidOperationException>().WithMessage("*Window functions require*");
   }

   [Fact]
   public void Should_reject_collection_parameters_on_servers_without_json_table()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", new MySqlServerVersion(new Version(8, 0, 3)), builder => builder.AddCollectionParameterSupport())
                    .Options;
      var act = () => new MySqlTestDbContext(options).Entities.ToQueryString();

      act.Should().Throw<InvalidOperationException>().WithMessage("*JSON_TABLE*");
   }
}
