namespace Thinktecture.EntityFrameworkCore.TenantDatabase;

public class TenantDatabaseTests : IntegrationTestsBase
{
   private string? _tenant;

   public TenantDatabaseTests(ITestOutputHelper testOutputHelper, SqlServerContainerFixture sqlServerContainerFixture)
      : base(testOutputHelper, sqlServerContainerFixture)
   {
      IsTenantDatabaseSupportEnabled = true;
      TenantDatabaseProviderMock.GetDatabaseName(Arg.Any<string>(), Arg.Any<string>()).Returns((string)null!);
      TenantDatabaseProviderMock.Tenant.Returns(_ => _tenant);
   }

   [Fact]
   public async Task Should_behave_the_same_if_no_tenant_and_database_provided()
   {
      TenantDatabaseProviderMock.GetDatabaseName(Schema, "TestEntities").Returns((string)null!);
      await ActDbContext.TestEntities.ToListAsync();

      ExecutedCommands.Last().Should().Contain($"FROM [{Schema}].[TestEntities]");
   }

   [Fact]
   public async Task Should_behave_the_same_if_database_name_is_specified_explicitly()
   {
      _tenant = "1";
      var database = ArrangeDbContext.Database.GetDbConnection().Database;
      TenantDatabaseProviderMock.GetDatabaseName(Schema, "TestEntities")
                                .Returns(database);

      await ActDbContext.TestEntities.ToListAsync();

      ExecutedCommands.Last().Should().Contain($"FROM [{database}].[{Schema}].[TestEntities]");
   }

   [Fact]
   public async Task Should_use_database_name_in_includes()
   {
      _tenant = "1";
      var database = ArrangeDbContext.Database.GetDbConnection().Database;
      TenantDatabaseProviderMock.GetDatabaseName(Schema, "TestEntities")
                                .Returns(database);

      await ActDbContext.TestEntities
                        .Include(t => t.Parent)
                        .Include(t => t.Children)
                        .ToListAsync();

      ExecutedCommands.Last().Should().Contain($"FROM [{database}].[{Schema}].[TestEntities]")
                      .And.Contain($"LEFT JOIN [{database}].[{Schema}].[TestEntities] AS [t0]")
                      .And.Contain($"LEFT JOIN [{database}].[{Schema}].[TestEntities] AS [t1]");
   }

   [Fact]
   public async Task Should_use_database_name_in_joins()
   {
      _tenant = "1";
      var database = ArrangeDbContext.Database.GetDbConnection().Database;
      TenantDatabaseProviderMock.GetDatabaseName(Schema, "TestEntities")
                                .Returns(database);

      await ActDbContext.TestEntities
                        .Join(ActDbContext.TestEntities, t => t.ParentId, t => t.Id, (t1, t2) => new { t1, t2 })
                        .ToListAsync();

      ExecutedCommands.Last().Should().Contain($"FROM [{database}].[{Schema}].[TestEntities]")
                      .And.Contain($"INNER JOIN [{database}].[{Schema}].[TestEntities]");
   }

   [Fact]
   public async Task Should_use_database_name_in_views()
   {
      _tenant = "1";
      var database = ArrangeDbContext.Database.GetDbConnection().Database;
      TenantDatabaseProviderMock.GetDatabaseName(Schema, "TestView")
                                .Returns(database);

      await ActDbContext.TestView.ToListAsync();

      ExecutedCommands.Last().Should().Contain($"FROM [{database}].[{Schema}].[TestView]");
   }
}
