using System.Transactions;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTransactionScopeTests : MySqlIntegrationTestsBase
{
   public MySqlTransactionScopeTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_insert_and_commit_inside_a_transaction_scope()
   {
      await using var context = await CreateContextAsync();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Scoped" }]);
         scope.Complete();
      }

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Scoped");
   }

   [Fact]
   public async Task Should_roll_back_an_insert_when_the_transaction_scope_is_not_completed()
   {
      await using var context = await CreateContextAsync();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Scoped" }]);
      }

      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_throw_NotSupportedException_when_upserting_inside_xa_transaction_scope()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Before" }]);

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var act = () => context.BulkInsertOrUpdateAsync(
                                                         [new MySqlTestEntity { Id = 1, Name = "After" }, new MySqlTestEntity { Id = 2, Name = "New" }],
                                                         propertiesToInsert: entity => new { entity.Id, entity.Name },
                                                         propertiesToUpdate: entity => entity.Name,
                                                         propertiesToMatchOn: entity => entity.Id);

         // MySQL rejects temporary tables inside an XA transaction (error 4091).
         await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*UseXaTransactions=false*");
      }

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).ToListAsync()).Should().Equal("Before");
   }

   [Fact]
   public async Task Should_throw_NotSupportedException_when_updating_inside_xa_transaction_scope()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Before" }]);

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var act = () => context.BulkUpdateAsync(
                                                 [new MySqlTestEntity { Id = 1, Name = "After" }],
                                                 entity => entity.Name,
                                                 entity => entity.Id);

         await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*UseXaTransactions=false*");
      }

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Before");
   }

   [Fact]
   public async Task Should_insert_into_the_enlisted_transaction_and_roll_back_with_it()
   {
      await using var context = await CreateContextAsync();

      using (var transaction = new CommittableTransaction())
      {
         // MySqlConnection.EnlistTransaction throws "Connection is not open." for a closed connection.
         await context.Database.OpenConnectionAsync();
         context.Database.EnlistTransaction(transaction);

         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Enlisted" }]);

         transaction.Rollback();
      }

      await context.Database.CloseConnectionAsync();
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_update_inside_the_enlisted_transaction_and_roll_back_with_it()
   {
      await using var context = await CreateContextWithoutXaTransactionsAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Before" }]);

      using (var transaction = new CommittableTransaction())
      {
         await context.Database.OpenConnectionAsync();
         context.Database.EnlistTransaction(transaction);

         var affectedRows = await context.BulkUpdateAsync(
                                                          [new MySqlTestEntity { Id = 1, Name = "After" }],
                                                          entity => entity.Name,
                                                          entity => entity.Id);
         affectedRows.Should().Be(1);

         transaction.Rollback();
      }

      await context.Database.CloseConnectionAsync();
      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be("Before");
   }

   [Fact]
   public async Task Should_commit_a_transaction_scope_without_xa_implicitly_when_truncating_with_throwOnTransaction_false()
   {
      await using var context = await CreateContextWithoutXaTransactionsAsync();
      context.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await context.SaveChangesAsync();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Inside" }]);
         await context.TruncateTableAsync<MySqlQuerySource>(throwOnTransaction: false);
      }

      // The scope is not completed, but TRUNCATE TABLE has committed the insert above.
      (await context.QuerySources.CountAsync()).Should().Be(0);
      (await context.Entities.AsNoTracking().Select(entity => entity.Name).ToListAsync()).Should().Equal("Inside");
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_update_inside_a_transaction_scope_without_xa(bool completeScope)
   {
      await using var context = await CreateContextWithoutXaTransactionsAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Before" }]);

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await context.BulkUpdateAsync(
                                                          [new MySqlTestEntity { Id = 1, Name = "After" }],
                                                          entity => entity.Name,
                                                          entity => entity.Id);

         affectedRows.Should().Be(1);

         if (completeScope)
            scope.Complete();
      }

      (await context.Entities.AsNoTracking().Select(entity => entity.Name).SingleAsync()).Should().Be(completeScope ? "After" : "Before");
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_upsert_inside_a_transaction_scope_without_xa(bool completeScope)
   {
      await using var context = await CreateContextWithoutXaTransactionsAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "Before" }]);

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await context.BulkInsertOrUpdateAsync(
                                                                  [new MySqlTestEntity { Id = 1, Name = "After" }, new MySqlTestEntity { Id = 2, Name = "New" }],
                                                                  propertiesToInsert: entity => new { entity.Id, entity.Name },
                                                                  propertiesToUpdate: entity => entity.Name,
                                                                  propertiesToMatchOn: entity => entity.Id);

         affectedRows.Should().Be(2);

         if (completeScope)
            scope.Complete();
      }

      var names = await context.Entities.AsNoTracking().OrderBy(entity => entity.Id).Select(entity => entity.Name).ToListAsync();

      if (completeScope)
         names.Should().Equal("After", "New");
      else
         names.Should().Equal("Before");
   }

   [Fact]
   public async Task Should_throw_when_truncating_inside_a_transaction_scope()
   {
      await using var context = await CreateContextAsync();
      await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "One" }, new MySqlTestEntity { Id = 2, Name = "Two" }]);

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         await context.Awaiting(ctx => ctx.TruncateTableAsync<MySqlTestEntity>())
                      .Should().ThrowAsync<InvalidOperationException>().WithMessage("*throwOnTransaction: false*");
      }

      (await context.Entities.CountAsync()).Should().Be(2);
   }

   [Fact]
   public async Task Should_surface_server_error_when_truncating_inside_xa_transaction_scope_with_throwOnTransaction_false()
   {
      await using var context = await CreateContextAsync();
      context.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source" });
      await context.SaveChangesAsync();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         // MySQL rejects a statement with an implicit commit inside an active XA transaction (error 1399).
         (await context.Awaiting(ctx => ctx.TruncateTableAsync<MySqlQuerySource>(throwOnTransaction: false))
                       .Should().ThrowAsync<MySqlConnector.MySqlException>())
            .Which.Number.Should().Be(1399);
      }

      (await context.QuerySources.CountAsync()).Should().Be(1);
   }

   // Without XA, MySqlConnector enlists the connection with a plain "START TRANSACTION", so temporary tables work.
   private async Task<MySqlTestDbContext> CreateContextWithoutXaTransactionsAsync()
   {
      var context = await CreateContextAsync();
      var builder = new MySqlConnector.MySqlConnectionStringBuilder(ConnectionString) { UseXaTransactions = false };
      context.Database.SetConnectionString(builder.ConnectionString);
      return context;
   }
}
