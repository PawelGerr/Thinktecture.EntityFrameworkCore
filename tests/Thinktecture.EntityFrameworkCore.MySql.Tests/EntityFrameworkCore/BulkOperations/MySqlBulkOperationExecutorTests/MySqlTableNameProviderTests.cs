using Microsoft.EntityFrameworkCore.Infrastructure;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlTableNameProviderTests : MySqlIntegrationTestsBase
{
   private const string _TARGET = "tn_target";
   private const string _OTHER_DATABASE = "other_database";
   private const string _TRANSLATED_TABLE = "other_database_Entities";

   // One instance for all tests: the translator is part of the key of the internal service provider of EF Core.
   private static readonly MySqlSchemaNameTranslator _translateSchema = (schema, name) => $"{schema}_{name}";

   public MySqlTableNameProviderTests(ITestOutputHelper testOutputHelper, MySqlContainerFixture fixture)
      : base(testOutputHelper, fixture)
   {
   }

   [Fact]
   public async Task Should_insert_into_the_table_of_the_provider()
   {
      await using var context = await CreateContextWithTargetTableAsync();

      await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
         [new MySqlTestEntity { Id = 1, Name = "a" }],
         new MySqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET) });

      (await CountAsync(context, _TARGET)).Should().Be(1);
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_upsert_into_the_table_of_the_provider()
   {
      await using var context = await CreateContextWithTargetTableAsync();
      var options = new MySqlBulkInsertOrUpdateOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET) };
      var executor = context.GetService<MySqlBulkOperationExecutor>();

      await executor.BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "a" }], options);
      await executor.BulkInsertOrUpdateAsync([new MySqlTestEntity { Id = 1, Name = "b" }], options);

      (await CountAsync(context, _TARGET)).Should().Be(1);
      (await context.Database.SqlQueryRaw<string>($"SELECT `Name` AS `Value` FROM `{_TARGET}`").SingleAsync()).Should().Be("b");
      (await context.Entities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_update_the_table_of_the_provider()
   {
      await using var context = await CreateContextWithTargetTableAsync();
      await context.Database.ExecuteSqlRawAsync($"INSERT INTO `{_TARGET}` (`Id`, `Name`) VALUES (1, 'before')");

      var affectedRows = await context.GetService<MySqlBulkOperationExecutor>().BulkUpdateAsync(
         [new MySqlTestEntity { Id = 1, Name = "after" }],
         new MySqlBulkUpdateOptions
         {
            PropertiesToUpdate = IEntityPropertiesProvider.Include<MySqlTestEntity>(entity => entity.Name),
            TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_TARGET)
         });

      affectedRows.Should().Be(1);
      (await context.Database.SqlQueryRaw<string>($"SELECT `Name` AS `Value` FROM `{_TARGET}`").SingleAsync()).Should().Be("after");
   }

   [Theory]
   [InlineData("insert", "ForRootEntity")]
   [InlineData("insert", "ForRootEntityWithDefaultName")]
   [InlineData("insert", "ObsoleteSchema")]
   [InlineData("update", "ForRootEntity")]
   [InlineData("update", "ForRootEntityWithDefaultName")]
   [InlineData("update", "ObsoleteSchema")]
   [InlineData("upsert", "ForRootEntity")]
   [InlineData("upsert", "ForRootEntityWithDefaultName")]
   [InlineData("upsert", "ObsoleteSchema")]
   public async Task Should_throw_if_the_table_name_has_a_schema_and_SchemaBehavior_is_Throw(string operation, string schemaSource)
   {
      await using var context = await CreateContextWithTargetTableAsync();
      await context.Database.ExecuteSqlRawAsync($"INSERT INTO `{_TARGET}` (`Id`, `Name`) VALUES (1, 'before')");
      await context.Database.ExecuteSqlRawAsync("INSERT INTO `Entities` (`Id`, `Name`) VALUES (1, 'before')");
      var executor = context.GetService<MySqlBulkOperationExecutor>();
      IBulkOperationTableNameProvider? provider = schemaSource switch
      {
         "ForRootEntity" => IBulkOperationTableNameProvider.ForRootEntity(_TARGET, _OTHER_DATABASE),
         "ForRootEntityWithDefaultName" => IBulkOperationTableNameProvider.ForRootEntityWithDefaultName(_OTHER_DATABASE),
         _ => null
      };
      MySqlTestEntity[] entities = [new() { Id = 1, Name = "after" }, new() { Id = 2, Name = "new" }];

#pragma warning disable CS0618 // The obsolete Schema is one of the tested sources.
      Func<Task> act = operation switch
      {
         "insert" => () => executor.BulkInsertAsync([entities[1]],
                                                    provider is null
                                                       ? new MySqlBulkInsertOptions { Schema = _OTHER_DATABASE }
                                                       : new MySqlBulkInsertOptions { TableNameProvider = provider }),
         "update" => () => executor.BulkUpdateAsync(entities,
                                                    provider is null
                                                       ? new MySqlBulkUpdateOptions { Schema = _OTHER_DATABASE }
                                                       : new MySqlBulkUpdateOptions { TableNameProvider = provider }),
         _ => () => executor.BulkInsertOrUpdateAsync(entities,
                                                     provider is null
                                                        ? new MySqlBulkInsertOrUpdateOptions { Schema = _OTHER_DATABASE }
                                                        : new MySqlBulkInsertOrUpdateOptions { TableNameProvider = provider })
      };
#pragma warning restore CS0618

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*SchemaBehavior*");
      (await LoadNamesAsync(context, _TARGET)).Should().Equal("before");
      (await LoadNamesAsync(context, "Entities")).Should().Equal("before");
   }

   [Fact]
   public async Task Should_throw_if_the_model_has_a_schema_and_SchemaBehavior_is_Throw()
   {
      await using (var arrangeContext = await CreateContextAsync())
      {
         await arrangeContext.Database.ExecuteSqlRawAsync("INSERT INTO `Entities` (`Id`, `Name`) VALUES (1, 'before')");
      }

      ConfigureModel = builder => builder.Entity<MySqlTestEntity>().ToTable("Entities", _OTHER_DATABASE);
      await using var context = await CreateContextAsync();

      await context.GetService<MySqlBulkOperationExecutor>()
                   .Awaiting(executor => executor.BulkInsertAsync([new MySqlTestEntity { Id = 2, Name = "new" }], new MySqlBulkInsertOptions()))
                   .Should().ThrowAsync<NotSupportedException>().WithMessage("*SchemaBehavior*");
      await context.Awaiting(ctx => ctx.TruncateTableAsync<MySqlTestEntity>())
                   .Should().ThrowAsync<NotSupportedException>().WithMessage("*SchemaBehavior*");

      (await LoadNamesAsync(context, "Entities")).Should().Equal("before");
   }

   [Fact]
   public async Task Should_throw_before_the_first_write_if_owned_type_table_has_a_schema()
   {
      await using var context = await CreateContextAsync();
      var entity = new TestEntity_Owns_SeparateOne
                   {
                      Id = new Guid("2F4B1E58-3C1D-4A55-9A0B-6E0F3C1D2A11"),
                      SeparateEntity = new OwnedEntity { IntColumn = 42, StringColumn = "value" }
                   };
      context.Add(entity);
      var provider = IBulkOperationTableNameProvider.Create(entityType => new BulkOperationTableName(entityType.GetTableName()!, entityType.IsOwned() ? _OTHER_DATABASE : null));

      // Inside the transaction of the caller the executor does not roll back, so an empty root table proves that nothing was written.
      await using var transaction = await context.Database.BeginTransactionAsync();

      await context.GetService<MySqlBulkOperationExecutor>()
                   .Awaiting(executor => executor.BulkInsertAsync([entity], new MySqlBulkInsertOptions { TableNameProvider = provider }))
                   .Should().ThrowAsync<NotSupportedException>().WithMessage("*SchemaBehavior*");

      (await CountAsync(context, "TestEntities_Own_SeparateOne")).Should().Be(0);
      (await CountAsync(context, "SeparateEntitiesOne")).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_into_the_translated_table_if_SchemaBehavior_is_Translate()
   {
      await using var context = await CreateContextWithTranslatedTableAsync();

      try
      {
         await context.GetService<MySqlBulkOperationExecutor>().BulkInsertAsync(
            [new MySqlTestEntity { Id = 1, Name = "a" }],
            new MySqlBulkInsertOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("Entities", _OTHER_DATABASE) });

         (await LoadNamesAsync(context, _TRANSLATED_TABLE)).Should().Equal("a");
         (await context.Entities.CountAsync()).Should().Be(0);
      }
      finally
      {
         await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{_TRANSLATED_TABLE}`");
      }
   }

   [Fact]
   public async Task Should_insert_from_query_into_the_translated_table_if_SchemaBehavior_is_Translate()
   {
      await using var context = await CreateContextWithTranslatedTableAsync();

      try
      {
         await context.Database.ExecuteSqlRawAsync("INSERT INTO `Entities` (`Id`, `Name`) VALUES (1, 'a')");

         var affectedRows = await context.Entities.BulkInsertAsync(
                               context.Entities,
                               builder => builder.Map(e => e.Id, s => s.Id).Map(e => e.Name, s => s.Name),
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("Entities", _OTHER_DATABASE) });

         affectedRows.Should().Be(1);
         (await LoadNamesAsync(context, _TRANSLATED_TABLE)).Should().Equal("a");
         ExecutedCommands.Should().Contain(command => command.StartsWith($"INSERT INTO `{_TRANSLATED_TABLE}` (", StringComparison.Ordinal));
      }
      finally
      {
         await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{_TRANSLATED_TABLE}`");
      }
   }

   [Fact]
   public async Task Should_update_from_query_the_translated_table_if_SchemaBehavior_is_Translate()
   {
      await using var context = await CreateContextWithTranslatedTableAsync();

      try
      {
         await context.Database.ExecuteSqlRawAsync("INSERT INTO `Entities` (`Id`, `Name`) VALUES (1, 'after')");
         await context.Database.ExecuteSqlRawAsync($"INSERT INTO `{_TRANSLATED_TABLE}` (`Id`, `Name`) VALUES (1, 'before')");

         var affectedRows = await context.Entities.BulkUpdateAsync(
                               context.Entities,
                               e => e.Id,
                               s => s.Id,
                               builder => builder.Set(e => e.Name, (e, s) => s.Name),
                               options: new MySqlBulkUpdateFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("Entities", _OTHER_DATABASE) });

         affectedRows.Should().Be(1);
         (await LoadNamesAsync(context, _TRANSLATED_TABLE)).Should().Equal("after");
         ExecutedCommands.Should().Contain(command => command.Contains($"UPDATE `{_TRANSLATED_TABLE}`", StringComparison.Ordinal));
      }
      finally
      {
         await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{_TRANSLATED_TABLE}`");
      }
   }

   private async Task<MySqlTestDbContext> CreateContextWithTranslatedTableAsync()
   {
      ConfigureMySql = builder => builder.SchemaBehavior(MySqlSchemaBehavior.Translate, _translateSchema);
      var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{_TRANSLATED_TABLE}`");
      await context.Database.ExecuteSqlRawAsync($"CREATE TABLE `{_TRANSLATED_TABLE}` LIKE `Entities`");
      return context;
   }

   private static async Task<List<string?>> LoadNamesAsync(MySqlTestDbContext context, string table)
   {
      return await context.Database.SqlQueryRaw<string?>($"SELECT `Name` AS `Value` FROM `{table}` ORDER BY `Id`").ToListAsync();
   }

   private async Task<MySqlTestDbContext> CreateContextWithTargetTableAsync()
   {
      var context = await CreateContextAsync();
      await context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS `{_TARGET}`");
      await context.Database.ExecuteSqlRawAsync($"CREATE TABLE `{_TARGET}` LIKE `Entities`");
      return context;
   }

   private static async Task<int> CountAsync(MySqlTestDbContext context, string table)
   {
      return await context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS `Value` FROM `{table}`").SingleAsync();
   }
}
