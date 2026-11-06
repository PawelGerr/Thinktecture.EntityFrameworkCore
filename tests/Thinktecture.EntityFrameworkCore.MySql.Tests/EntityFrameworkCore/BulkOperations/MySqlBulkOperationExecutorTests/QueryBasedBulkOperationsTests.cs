using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class QueryBasedBulkOperationsTests
{
   [Fact]
   public void Should_generate_insert_select_with_mapped_columns()
   {
      using var context = CreateContext();
      using var loggerFactory = LoggerFactory.Create(_ => { });
      var translator = new EfCoreValueExpressionTranslator(loggerFactory.CreateLogger("MySQL tests"), context);
      var mapping = new InsertPropertyBuilder<MySqlTestEntity, MySqlTestEntity>()
                    .Map(target => target.Name, source => source.Name);

      var (sql, parameters) = translator.TranslateInsertFromQuery<MySqlTestEntity, MySqlTestEntity>(
         context.Entities,
         mapping.Entries,
         IBulkOperationTableNameProvider.ForRootEntity("insert_target"));

      sql.Should().Contain("INSERT INTO `insert_target` (`Name`)");
      sql.Should().Contain("SELECT");
      parameters.Should().BeEmpty();
   }

   [Fact]
   public void Should_generate_update_join_with_filter()
   {
      using var context = CreateContext();
      using var loggerFactory = LoggerFactory.Create(_ => { });
      var translator = new EfCoreValueExpressionTranslator(loggerFactory.CreateLogger("MySQL tests"), context);
      var assignments = new SetPropertyBuilder<MySqlTestEntity, MySqlTestEntity>()
                        .Set(target => target.Name, (target, source) => source.Name);
      Expression<Func<MySqlTestEntity, MySqlTestEntity, bool>> filter = (target, source) => target.Id < source.Id;

      var (sql, _) = translator.TranslateUpdateFromQuery<MySqlTestEntity, MySqlTestEntity, int>(
         context.Entities,
         target => target.Id,
         source => source.Id,
         assignments.Entries,
         filter,
         IBulkOperationTableNameProvider.Default);

      sql.Should().Contain("UPDATE");
      sql.Should().Contain("JOIN");
      sql.Should().Contain(" ON ");
      sql.Should().Contain("SET");
      sql.Should().Contain("WHERE");
   }

   [Fact]
   public async Task Should_reject_query_insert_without_mappings()
   {
      var act = async () =>
      {
         using var context = CreateContext();
         var executor = context.GetService<MySqlBulkOperationExecutor>();
         await executor.BulkInsertAsync<MySqlTestEntity, MySqlTestEntity>(context.Entities, builder => builder);
      };

      await act.Should().ThrowAsync<ArgumentException>();
   }

   [Fact]
   public async Task Should_reject_query_update_without_assignments()
   {
      var act = async () =>
      {
         using var context = CreateContext();
         var executor = context.GetService<MySqlBulkOperationExecutor>();
         await executor.BulkUpdateAsync<MySqlTestEntity, MySqlTestEntity, int>(
            context.Entities,
            target => target.Id,
            source => source.Id,
            builder => builder);
      };

      await act.Should().ThrowAsync<ArgumentException>();
   }

   private static MySqlTestDbContext CreateContext()
   {
      var options = new DbContextOptionsBuilder<MySqlTestDbContext>()
                    .UseMySql("Server=localhost;Database=test", MySqlContainerFixture.ServerVersion, builder => builder.AddBulkOperationSupport())
                    .Options;

      return new MySqlTestDbContext(options);
   }
}
