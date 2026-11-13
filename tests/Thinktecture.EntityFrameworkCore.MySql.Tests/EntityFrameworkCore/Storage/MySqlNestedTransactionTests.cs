using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Storage;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlNestedTransactionTests : MySqlIntegrationTestsBase
{
   public MySqlNestedTransactionTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_commit_the_root_transaction_after_a_nested_one()
   {
      await using var context = await CreateNestedContextAsync();

      await using (var root = await context.Database.BeginTransactionAsync())
      {
         context.Entities.Add(new MySqlTestEntity { Id = 1, Name = "root" });
         await context.SaveChangesAsync();

         await using (var nested = await context.Database.BeginTransactionAsync())
         {
            context.Entities.Add(new MySqlTestEntity { Id = 2, Name = "nested" });
            await context.SaveChangesAsync();
            await nested.CommitAsync();
         }

         await root.CommitAsync();
      }

      (await context.Entities.AsNoTracking().CountAsync()).Should().Be(2);
   }

   [Fact]
   public async Task Should_roll_back_nested_work_with_the_root_transaction()
   {
      await using var context = await CreateNestedContextAsync();

      await using (var root = await context.Database.BeginTransactionAsync())
      {
         await using (var nested = await context.Database.BeginTransactionAsync())
         {
            context.Entities.Add(new MySqlTestEntity { Id = 1, Name = "nested" });
            await context.SaveChangesAsync();
            await nested.CommitAsync();
         }

         await root.RollbackAsync();
      }

      (await context.Entities.AsNoTracking().CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_run_bulk_operations_inside_a_nested_transaction()
   {
      await using var context = await CreateNestedContextAsync();

      await using (var root = await context.Database.BeginTransactionAsync())
      {
         await using var nested = await context.Database.BeginTransactionAsync();
         await context.BulkInsertAsync([new MySqlTestEntity { Id = 1, Name = "bulk" }]);
         await nested.CommitAsync();
         await root.RollbackAsync();
      }

      (await context.Entities.AsNoTracking().CountAsync()).Should().Be(0);
   }

   private async Task<MySqlTestDbContext> CreateNestedContextAsync()
   {
      // The base creates the schema and clears the tables.
      await using (await CreateContextAsync())
      {
      }

      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql(ConnectionString, MySqlContainerFixture.ServerVersion, builder => builder.AddBulkOperationSupport())
                    .AddNestedTransactionSupport()
                    .Options;

      return new MySqlTestDbContext(options);
   }
}
