using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.QueryableExtensionsTests;

public class WithTableHints : IntegrationTestsBase
{
   private string? _escapedSchema;
   private string EscapedSchema => _escapedSchema ??= $"[{ActDbContext.Schema}]";

   public WithTableHints(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public async Task Should_add_table_hints_to_table()
   {
      var query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock);

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)");

      var result = await query.ToListAsync();
      result.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_escape_index_name()
   {
      var query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.Index("IX_TestEntities_Id"));

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (INDEX([IX_TestEntities_Id]))");

      var result = await query.ToListAsync();
      result.Should().BeEmpty();
   }

   [Fact]
   public void Should_not_mess_up_table_hints()
   {
      var query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock);

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)");

      query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.UpdLock);

      query.ToQueryString().Should().Be(@"SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (UPDLOCK)");
   }

   [Fact]
   public async Task Should_add_table_hints_to_table_without_touching_included_navigations()
   {
      var query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock)
                              .Include(e => e.Children);

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField], [t0].[Id], [t0].[ConvertibleClass], [t0].[Count], [t0].[Name], [t0].[ParentId], [t0].[PropertyWithBackingField], [t0].[RequiredName], [t0].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)" + Environment.NewLine +
                                        $"LEFT JOIN {EscapedSchema}.[TestEntities] AS [t0] ON [t].[Id] = [t0].[ParentId]" + Environment.NewLine +
                                        "ORDER BY [t].[Id]");

      var result = await query.ToListAsync();
      result.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_work_with_joins_on_itself()
   {
      var query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock)
                              .Join(ActDbContext.TestEntities, e => e.Id, e => e.Id, (e1, e2) => new { e1, e2 });

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField], [t0].[Id], [t0].[ConvertibleClass], [t0].[Count], [t0].[Name], [t0].[ParentId], [t0].[PropertyWithBackingField], [t0].[RequiredName], [t0].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)" + Environment.NewLine +
                                        $"INNER JOIN {EscapedSchema}.[TestEntities] AS [t0] ON [t].[Id] = [t0].[Id]");

      (await query.ToListAsync()).Should().BeEmpty();

      query = ActDbContext.TestEntities
                          .Join(ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock), e => e.Id, e => e.Id, (e1, e2) => new { e1, e2 });

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField], [t0].[Id], [t0].[ConvertibleClass], [t0].[Count], [t0].[Name], [t0].[ParentId], [t0].[PropertyWithBackingField], [t0].[RequiredName], [t0].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t]" + Environment.NewLine +
                                        $"INNER JOIN {EscapedSchema}.[TestEntities] AS [t0] WITH (NOLOCK) ON [t].[Id] = [t0].[Id]");

      (await query.ToListAsync()).Should().BeEmpty();

      query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock)
                          .Join(ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock), e => e.Id, e => e.Id, (e1, e2) => new { e1, e2 });

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField], [t0].[Id], [t0].[ConvertibleClass], [t0].[Count], [t0].[Name], [t0].[ParentId], [t0].[PropertyWithBackingField], [t0].[RequiredName], [t0].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)" + Environment.NewLine +
                                        $"INNER JOIN {EscapedSchema}.[TestEntities] AS [t0] WITH (NOLOCK) ON [t].[Id] = [t0].[Id]");

      (await query.ToListAsync()).Should().BeEmpty();

      query = ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.NoLock)
                          .Join(ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.UpdLock), e => e.Id, e => e.Id, (e1, e2) => new { e1, e2 });

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t].[ConvertibleClass], [t].[Count], [t].[Name], [t].[ParentId], [t].[PropertyWithBackingField], [t].[RequiredName], [t].[_privateField], [t0].[Id], [t0].[ConvertibleClass], [t0].[Count], [t0].[Name], [t0].[ParentId], [t0].[PropertyWithBackingField], [t0].[RequiredName], [t0].[_privateField]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)" + Environment.NewLine +
                                        $"INNER JOIN {EscapedSchema}.[TestEntities] AS [t0] WITH (UPDLOCK) ON [t].[Id] = [t0].[Id]");

      (await query.ToListAsync()).Should().BeEmpty();

      var tempQuery = ActDbContext.TestEntities.Select(e => new TestEntity { Id = e.Id });
      query = tempQuery.WithTableHints(SqlServerTableHint.NoLock)
                       .Join(tempQuery.WithTableHints(SqlServerTableHint.UpdLock), e => e.Id, e => e.Id, (e1, e2) => new { e1, e2 });

      query.ToQueryString().Should().Be("SELECT [t].[Id], [t0].[Id]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities] AS [t] WITH (NOLOCK)" + Environment.NewLine +
                                        $"INNER JOIN {EscapedSchema}.[TestEntities] AS [t0] WITH (UPDLOCK) ON [t].[Id] = [t0].[Id]");

      (await query.ToListAsync()).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_add_table_hints_to_table_and_owned_entities()
   {
      var query = ActDbContext.TestEntities_Own_SeparateMany_SeparateMany.WithTableHints(SqlServerTableHint.NoLock);

      query.ToQueryString().Should().Be(@"SELECT [t].[Id], [t0].[TestEntity_Owns_SeparateMany_SeparateManyId], [t0].[Id], [t0].[IntColumn], [t0].[StringColumn], [t0].[OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId], [t0].[OwnedEntity_Owns_SeparateManyId], [t0].[Id0], [t0].[IntColumn0], [t0].[StringColumn0]" + Environment.NewLine +
                                        $"FROM {EscapedSchema}.[TestEntities_Own_SeparateMany_SeparateMany] AS [t] WITH (NOLOCK)" + Environment.NewLine +
                                        "LEFT JOIN (" + Environment.NewLine +
                                        "    SELECT [s].[TestEntity_Owns_SeparateMany_SeparateManyId], [s].[Id], [s].[IntColumn], [s].[StringColumn], [s0].[OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId], [s0].[OwnedEntity_Owns_SeparateManyId], [s0].[Id] AS [Id0], [s0].[IntColumn] AS [IntColumn0], [s0].[StringColumn] AS [StringColumn0]" + Environment.NewLine +
                                        $"    FROM {EscapedSchema}.[SeparateEntitiesMany_SeparateEntitiesMany] AS [s] WITH (NOLOCK)" + Environment.NewLine +
                                        $"    LEFT JOIN {EscapedSchema}.[SeparateEntitiesMany_SeparateEntitiesMany_Inner] AS [s0] WITH (NOLOCK) ON [s].[TestEntity_Owns_SeparateMany_SeparateManyId] = [s0].[OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId] AND [s].[Id] = [s0].[OwnedEntity_Owns_SeparateManyId]" + Environment.NewLine +
                                        ") AS [t0] ON [t].[Id] = [t0].[TestEntity_Owns_SeparateMany_SeparateManyId]" + Environment.NewLine +
                                        "ORDER BY [t].[Id], [t0].[TestEntity_Owns_SeparateMany_SeparateManyId], [t0].[Id], [t0].[OwnedEntity_Owns_SeparateManyTestEntity_Owns_SeparateMany_SeparateManyId], [t0].[OwnedEntity_Owns_SeparateManyId]");

      var result = await query.ToListAsync();
      result.Should().BeEmpty();
   }

   [Fact]
   public async Task Should_add_table_hints_to_temp_table()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { Guid.Empty });
      var query = tempTable.Query.WithTableHints(SqlServerTableHint.UpdLock, SqlServerTableHint.RowLock);

      query.ToQueryString().Should().Be(@"SELECT [#].[Column1]" + Environment.NewLine +
                                        "FROM [#TempTable<Guid>_1] AS [#] WITH (UPDLOCK, ROWLOCK)");

      var result = await query.ToListAsync();
      result.Should().HaveCount(1);
      result[0].Should().Be(Guid.Empty);

      var joinQuery = tempTable.Query.WithTableHints(SqlServerTableHint.UpdLock, SqlServerTableHint.RowLock)
                               .LeftJoin(tempTable.Query.WithTableHints(SqlServerTableHint.UpdLock),
                                         e => e, e => e);

      joinQuery.ToQueryString().Should().Be(@"SELECT [#].[Column1] AS [Left], [#0].[Column1] AS [Right]" + Environment.NewLine +
                                            "FROM [#TempTable<Guid>_1] AS [#] WITH (UPDLOCK, ROWLOCK)" + Environment.NewLine +
                                            "LEFT JOIN [#TempTable<Guid>_1] AS [#0] WITH (UPDLOCK) ON [#].[Column1] = [#0].[Column1]");

      joinQuery = tempTable.Query.WithTableHints(SqlServerTableHint.UpdLock, SqlServerTableHint.RowLock)
                           .LeftJoin(tempTable.Query.WithTableHints(SqlServerTableHint.UpdLock, SqlServerTableHint.RowLock),
                                     e => e, e => e);

      joinQuery.ToQueryString().Should().Be(@"SELECT [#].[Column1] AS [Left], [#0].[Column1] AS [Right]" + Environment.NewLine +
                                            "FROM [#TempTable<Guid>_1] AS [#] WITH (UPDLOCK, ROWLOCK)" + Environment.NewLine +
                                            "LEFT JOIN [#TempTable<Guid>_1] AS [#0] WITH (UPDLOCK, ROWLOCK) ON [#].[Column1] = [#0].[Column1]");
   }

   [Fact]
   public async Task Should_work_with_projection_using_SplitQuery_behavior()
   {
      ConfigureModel = builder => builder.ConfigureTempTable<Guid>();

      var parentId = new Guid("6EB48130-454B-46BC-9FEE-CDF411F69807");
      var childId = new Guid("17A2014C-F7B2-40E8-82F4-42E754DCAA2D");
      ArrangeDbContext.TestEntities.Add(new TestEntity
                                        {
                                           Id = parentId,
                                           RequiredName = "RequiredName",
                                           Children = { new() { Id = childId, RequiredName = "RequiredName" } }
                                        });
      await ArrangeDbContext.SaveChangesAsync();

      await using var tempTable = await ActDbContext.BulkInsertValuesIntoTempTableAsync(new[] { parentId });

      var result = await ActDbContext.TestEntities.WithTableHints(SqlServerTableHint.UpdLock, SqlServerTableHint.RowLock)
                                     .AsSplitQuery()
                                     .Where(c => tempTable.Query
                                                          .WithTableHints(SqlServerTableHint.UpdLock)
                                                          .Any(id => c.Id == id))
                                     .Select(c => new
                                                  {
                                                     c.Id,
                                                     ChildrenIds = c.Children.Select(o => o.Id).ToList()
                                                  })
                                     .ToListAsync();

      result.Should().BeEquivalentTo(new[]
                                     {
                                        new
                                        {
                                           Id = parentId,
                                           ChildrenIds = new List<Guid> { childId }
                                        }
                                     });

      ExecutedCommands.Skip(ExecutedCommands.Count - 2).First().Should().Be(@"SELECT [t].[Id]" + Environment.NewLine +
                                                                            $"FROM [{TestCtxProvider.Schema}].[TestEntities] AS [t] WITH (UPDLOCK, ROWLOCK)" + Environment.NewLine +
                                                                            "WHERE [t].[Id] IN (" + Environment.NewLine +
                                                                            "    SELECT [#].[Column1]" + Environment.NewLine +
                                                                            "    FROM [#TempTable<Guid>_1] AS [#] WITH (UPDLOCK)" + Environment.NewLine +
                                                                            ")" + Environment.NewLine +
                                                                            "ORDER BY [t].[Id]");
      ExecutedCommands.Last().Should().Be("SELECT [t0].[Id], [t].[Id]" + Environment.NewLine +
                                          $"FROM [{TestCtxProvider.Schema}].[TestEntities] AS [t] WITH (UPDLOCK, ROWLOCK)" + Environment.NewLine +
                                          $"INNER JOIN [{TestCtxProvider.Schema}].[TestEntities] AS [t0] ON [t].[Id] = [t0].[ParentId]" + Environment.NewLine +
                                          "WHERE [t].[Id] IN (" + Environment.NewLine +
                                          "    SELECT [#].[Column1]" + Environment.NewLine +
                                          "    FROM [#TempTable<Guid>_1] AS [#] WITH (UPDLOCK)" + Environment.NewLine +
                                          ")" + Environment.NewLine +
                                          "ORDER BY [t].[Id]");
   }
}
