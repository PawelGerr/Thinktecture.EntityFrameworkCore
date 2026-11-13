using System.Text.RegularExpressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Thinktecture.EntityFrameworkCore.TempTables;
using Thinktecture.TestDatabaseContext;

#pragma warning disable CS0618 // TableName and Schema are obsolete; one test covers the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.MySqlBulkOperationExecutorTests;

// "INSERT INTO target (columns) SELECT ... FROM source" is executed by EF Core (ExecuteSqlRawAsync), so the statement is visible in ExecutedCommands.
// ExecutedCommands contains the setup statements of the tests as well, so the tests pick the statement by its prefix.
// A single statement is atomic, i.e. a failing statement leaves the target table unchanged.
// A MySQL temp table can be referenced once per statement, which is all that "INSERT ... SELECT" needs.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class BulkInsertFromQueryAsync : MySqlIntegrationTestsBase
{
   private const string _REDIRECT_TABLE = "TestEntities_QueryInsertRedirect";

   private static readonly Guid _TEST_ENTITY_ID = new("40B5CA93-5C02-48AD-B8A1-12BC13313866");
   private static readonly Guid _OTHER_TEST_ENTITY_ID = new("8AF163D7-D316-4B2D-A62F-6326A80C8BEE");
   private static readonly Guid _THIRD_TEST_ENTITY_ID = new("506E664A-9ADC-4221-9577-71DCFD73DE64");
   private static readonly ConvertibleClass _staticConvertibleClass = new(42);

   private MySqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<MySqlBulkOperationExecutor>();

   public BulkInsertFromQueryAsync(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_insert_entities_from_temp_table()
   {
      var sourceEntities = new[]
                           {
                              new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "Req1", Count = 10 },
                              new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name2", RequiredName = "Req2", Count = 20 }
                           };
      await using var tempTable = await CreateSourceAsync(sourceEntities);

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(tempTable.Query, MapAllColumns);

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Should().BeEquivalentTo(sourceEntities, config => config.WithStrictOrdering());
   }

   [Fact]
   public async Task Should_insert_multiple_properties()
   {
      var entity = new TestEntity
                   {
                      Id = _TEST_ENTITY_ID,
                      Name = "TestName",
                      RequiredName = "ReqName",
                      Count = 42,
                      NullableCount = 7,
                      PropertyWithBackingField = 3
                   };
      entity.SetPrivateField(5);
      await using var tempTable = await CreateSourceAsync(entity);

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                            tempTable.Query,
                            builder => MapAllColumns(builder).Map(e => e.NullableCount, f => f.NullableCount));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.AsNoTracking().SingleAsync();
      loadedEntity.Should().BeEquivalentTo(entity);
      loadedEntity.GetPrivateField().Should().Be(5);
   }

   [Fact]
   public async Task Should_insert_null_values()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = null, RequiredName = "ReqName", NullableCount = null });

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                            tempTable.Query,
                            builder => MapAllColumns(builder).Map(e => e.NullableCount, f => f.NullableCount));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.Name.Should().BeNull();
      loadedEntity.NullableCount.Should().BeNull();
   }

   [Fact]
   public async Task Should_insert_private_field_via_ef_property()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "Req1", Count = 10 };
      entity.SetPrivateField(42);
      await using var tempTable = await CreateSourceAsync(entity);

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(tempTable.Query, MapAllColumns);

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntities.SingleAsync();
      loadedEntity.GetPrivateField().Should().Be(42);
   }

   [Fact]
   public async Task Should_return_correct_affected_row_count()
   {
      await using var tempTable = await CreateSourceAsync(
                                     new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "Req1", Count = 1 },
                                     new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name2", RequiredName = "Req2", Count = 2 },
                                     new TestEntity { Id = _THIRD_TEST_ENTITY_ID, Name = "Name3", RequiredName = "Req3", Count = 3 });

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(tempTable.Query, MapAllColumns);

      affectedRows.Should().Be(3);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(3);
   }

   [Fact]
   public async Task Should_return_0_if_temp_table_is_empty()
   {
      await using var tempTable = await CreateSourceAsync<TestEntity>();

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(tempTable.Query, MapAllColumns);

      affectedRows.Should().Be(0);
      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task Should_return_0_if_source_query_returns_no_rows(bool useConstantPredicate)
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source", Count = 5 });
      await ArrangeDbContext.SaveChangesAsync();

      var sourceQuery = useConstantPredicate
                           ? ActDbContext.QuerySources.Where(s => false)
                           : ActDbContext.QuerySources.Where(s => s.Count > 100);

      var affectedRows = await ActDbContext.QueryTargets.BulkInsertAsync(
                            sourceQuery,
                            builder => builder.Map(t => t.Id, s => s.Id)
                                              .Map(t => t.Name, s => s.Name)
                                              .Map(t => t.Count, s => s.Count));

      affectedRows.Should().Be(0);
      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_generate_correct_sql_shape()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "Req1" });

      await ActDbContext.TestEntities.BulkInsertAsync(tempTable.Query, MapAllColumns);

      var statement = GetExecutedStatement("INSERT INTO ");
      statement.Should().StartWith("INSERT INTO `TestEntities` (`Id`, `RequiredName`, `Name`, `Count`, `PropertyWithBackingField`, `_privateField`) SELECT ");
      statement.Should().Contain($" FROM `{tempTable.Name}` AS `t`");
      statement.Should().NotContain(" WHERE ");
   }

   [Fact]
   public async Task Should_insert_from_regular_dbset_query()
   {
      ArrangeDbContext.TestEntities.AddRange(new TestEntity { Id = _TEST_ENTITY_ID, Name = "OrigName", RequiredName = "ReqName", Count = 99 },
                                             new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Other", RequiredName = "ReqName", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         var sourceQuery = ActDbContext.TestEntities
                                       .Where(e => e.Id == _TEST_ENTITY_ID)
                                       .Select(e => new { e.Id, e.RequiredName, e.Name, e.Count });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               sourceQuery,
                               builder => builder.Map(e => e.Id, f => f.Id)
                                                 .Map(e => e.RequiredName, f => f.RequiredName)
                                                 .Map(e => e.Name, f => f.Name)
                                                 .Map(e => e.Count, f => f.Count)
                                                 .Map(e => e.PropertyWithBackingField, f => 1)
                                                 .Map(e => EF.Property<int>(e, "_privateField"), f => 2),
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("OrigName");
         (await LoadColumnAsync<int>(_REDIRECT_TABLE, "PropertyWithBackingField")).Should().Equal(1);
         (await LoadColumnAsync<int>(_REDIRECT_TABLE, "_privateField")).Should().Equal(2);
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(2, "the source table is not changed");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_copies_of_the_rows_of_the_same_table()
   {
      var entity = new TestEntity { Id = _TEST_ENTITY_ID, Name = "Original", RequiredName = "ReqName", Count = 3, PropertyWithBackingField = 4 };
      entity.SetPrivateField(5);
      ArrangeDbContext.TestEntities.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      // MySQL materializes the rows to be inserted if source and target are the same table.
      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                            ActDbContext.TestEntities,
                            builder => builder.Map(e => e.Id, f => Guid.NewGuid())
                                              .Map(e => e.RequiredName, f => f.RequiredName)
                                              .Map(e => e.Name, f => f.Name + " (copy)")
                                              .Map(e => e.Count, f => f.Count)
                                              .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
                                              .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")));

      affectedRows.Should().Be(1);

      var loadedEntities = await AssertDbContext.TestEntities.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities[0].Should().BeEquivalentTo(entity);
      loadedEntities[1].Id.Should().NotBe(Guid.Empty).And.NotBe(_TEST_ENTITY_ID);
      loadedEntities[1].Name.Should().Be("Original (copy)");
      loadedEntities[1].Count.Should().Be(3);
      loadedEntities[1].PropertyWithBackingField.Should().Be(4);
      loadedEntities[1].GetPrivateField().Should().Be(5);
   }

   [Fact]
   public async Task Should_apply_parameters_of_the_source_query()
   {
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 1, Name = "Source 1", Count = 1 },
                                             new MySqlQuerySource { Id = 2, Name = "Source 2", Count = 5 },
                                             new MySqlQuerySource { Id = 3, Name = "Source 3", Count = 6 },
                                             new MySqlQuerySource { Id = 4, Name = "Source 4", Count = 9 });
      await ArrangeDbContext.SaveChangesAsync();
      var minCount = 5;
      var take = 2;

      var affectedRows = await ActDbContext.QueryTargets.BulkInsertAsync(
                            ActDbContext.QuerySources.Where(s => s.Count >= minCount).OrderBy(s => s.Count).Take(take),
                            builder => builder.Map(t => t.Id, s => s.Id)
                                              .Map(t => t.Name, s => s.Name)
                                              .Map(t => t.Count, s => s.Count));

      affectedRows.Should().Be(2);

      var loadedTargets = await AssertDbContext.QueryTargets.OrderBy(t => t.Id).ToListAsync();
      loadedTargets.Select(t => t.Name).Should().Equal("Source 2", "Source 3");
   }

   [Fact]
   public async Task Should_insert_entities_in_table_name_override()
   {
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "InsertedName", Count = 99 });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               tempTable.Query,
                               MapAllColumns,
                               new MySqlBulkInsertFromQueryOptions { TableName = _REDIRECT_TABLE });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("InsertedName");
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_entities_in_table_from_ForRootEntity_provider()
   {
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "InsertedName", Count = 99 });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               tempTable.Query,
                               MapAllColumns,
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("InsertedName");
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         GetExecutedStatement("INSERT INTO ").Should().StartWith($"INSERT INTO `{_REDIRECT_TABLE}` (");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_entities_in_table_of_the_database_from_ForRootEntity_provider()
   {
      // MySQL has no schemas below the database, so the schema of the table name is the name of the database (here: the current one).
      var database = await ActDbContext.Database.SqlQueryRaw<string>("SELECT DATABASE() AS `Value`").SingleAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "InsertedName" });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               tempTable.Query,
                               MapAllColumns,
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE, database) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("InsertedName");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact(Skip = "Known limitation: the schema of a table name is dropped by DelimitIdentifier of the MySQL SQL generation helper (Pomelo SchemaBehavior), so the statement runs against the current database without an error.")]
   public async Task Should_not_write_to_the_current_database_if_the_schema_of_the_table_name_names_another_database()
   {
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "InsertedName" });

         // The schema of a MySQL table name is the name of the database. The statement has to address that database (or fail),
         // it must not silently write to the table of the same name in the current database.
         var act = () => ActDbContext.TestEntities.BulkInsertAsync(
                      tempTable.Query,
                      MapAllColumns,
                      new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE, "other_database") });

         await act.Should().ThrowAsync<Exception>();
         (await LoadColumnAsync<Guid>(_REDIRECT_TABLE, "Id")).Should().BeEmpty();
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_entities_in_table_from_table_name_provider()
   {
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName", Name = "InsertedName" });
         var provider = new SuffixTableNameProvider("_QueryInsertRedirect");

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               tempTable.Query,
                               MapAllColumns,
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = provider });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("InsertedName");
         (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0, "original table should be empty");
         provider.RequestedEntityTypes.Should().ContainSingle().Which.ClrType.Should().Be(typeof(TestEntity));
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_escape_table_name_of_table_name_provider()
   {
      const string tableName = "TestEntities`; DROP TABLE TestEntities; -- Redirect";
      await CreateTableCopyAsync("TestEntities", tableName);

      try
      {
         await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               tempTable.Query,
                               MapAllColumns,
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(tableName) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<Guid>(tableName, "Id")).Should().Equal(_TEST_ENTITY_ID);
      }
      finally
      {
         await DropTablesAsync(tableName);
      }
   }

   [Fact]
   public async Task Should_insert_with_constant_value()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "OrigName", RequiredName = "ReqName", Count = 99 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         var sourceQuery = ActDbContext.TestEntities.Select(e => new { e.Id, e.RequiredName, e.Name });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               sourceQuery,
                               builder => builder.Map(e => e.Id, f => f.Id)
                                                 .Map(e => e.RequiredName, f => f.RequiredName)
                                                 .Map(e => e.Name, f => f.Name)
                                                 .Map(e => e.Count, f => 42)
                                                 .Map(e => e.PropertyWithBackingField, f => 0)
                                                 .Map(e => EF.Property<int>(e, "_privateField"), f => 0),
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<int>(_REDIRECT_TABLE, "Count")).Should().Equal(42);
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_with_captured_variable()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "OrigName", RequiredName = "ReqName", Count = 99 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         var sourceQuery = ActDbContext.TestEntities.Select(e => new { e.Id, e.RequiredName, e.Count });
         var capturedName = "CapturedValue";

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               sourceQuery,
                               builder => builder.Map(e => e.Id, f => f.Id)
                                                 .Map(e => e.RequiredName, f => f.RequiredName)
                                                 .Map(e => e.Name, f => capturedName)
                                                 .Map(e => e.Count, f => f.Count)
                                                 .Map(e => e.PropertyWithBackingField, f => 0)
                                                 .Map(e => EF.Property<int>(e, "_privateField"), f => 0),
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<string>(_REDIRECT_TABLE, "Name")).Should().Equal("CapturedValue");
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_captured_string_with_special_characters_as_is()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      var capturedName = "O'Reilly \\ \"quoted\" `tick`; DROP TABLE QueryTargets; -- \\'";

      var affectedRows = await ActDbContext.QueryTargets.BulkInsertAsync(
                            ActDbContext.QuerySources,
                            builder => builder.Map(t => t.Id, s => s.Id)
                                              .Map(t => t.Name, s => capturedName)
                                              .Map(t => t.Count, s => s.Count));

      affectedRows.Should().Be(1);
      (await AssertDbContext.QueryTargets.Select(t => t.Name).SingleAsync()).Should().Be(capturedName);
   }

   // The literal "{0}" is part of the generated SQL. It must neither be rejected nor be replaced by a parameter, which happens to the placeholders of a format string.
   [Fact]
   public async Task Should_insert_from_source_query_that_contains_curly_braces_in_string_literal()
   {
      await ArrangeSourcesWithCurlyBracesAsync();

      var affectedRows = await ActDbContext.QueryTargets.BulkInsertAsync(
                            ActDbContext.QuerySources.Where(s => s.Name != "{0}"),
                            builder => builder.Map(t => t.Id, s => s.Id)
                                              .Map(t => t.Name, s => s.Name)
                                              .Map(t => t.Count, s => s.Count));

      affectedRows.Should().Be(2);
      (await AssertDbContext.QueryTargets.OrderBy(t => t.Id).Select(t => t.Id).ToListAsync()).Should().Equal(2, 3);
   }

   [Fact]
   public async Task Should_insert_from_source_query_that_contains_curly_braces_in_string_literal_if_a_constant_is_mapped()
   {
      await ArrangeSourcesWithCurlyBracesAsync();

      var affectedRows = await ActDbContext.QueryTargets.BulkInsertAsync(
                            ActDbContext.QuerySources.Where(s => s.Name != "{0}"),
                            builder => builder.Map(t => t.Id, s => s.Id)
                                              .Map(t => t.Name, s => s.Name)
                                              .Map(t => t.Count, s => 42));

      affectedRows.Should().Be(2);
      (await AssertDbContext.QueryTargets.OrderBy(t => t.Id).Select(t => t.Id).ToListAsync()).Should().Equal(2, 3);
   }

   [Fact]
   public async Task Should_insert_with_arithmetic_expression()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = _TEST_ENTITY_ID, Name = "OrigName", RequiredName = "ReqName", Count = 7 });
      await ArrangeDbContext.SaveChangesAsync();
      await CreateTableCopyAsync("TestEntities", _REDIRECT_TABLE);

      try
      {
         var sourceQuery = ActDbContext.TestEntities.Select(e => new { e.Id, e.RequiredName, e.Name, e.Count });

         var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                               sourceQuery,
                               builder => builder.Map(e => e.Id, f => f.Id)
                                                 .Map(e => e.RequiredName, f => f.RequiredName)
                                                 .Map(e => e.Name, f => f.Name)
                                                 .Map(e => e.Count, f => f.Count * 2 + 1)
                                                 .Map(e => e.PropertyWithBackingField, f => 0)
                                                 .Map(e => EF.Property<int>(e, "_privateField"), f => 0),
                               new MySqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity(_REDIRECT_TABLE) });

         affectedRows.Should().Be(1);
         (await LoadColumnAsync<int>(_REDIRECT_TABLE, "Count")).Should().Equal(15);
      }
      finally
      {
         await DropTablesAsync(_REDIRECT_TABLE);
      }
   }

   [Fact]
   public async Task Should_insert_with_string_to_upper()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "lowercase", RequiredName = "ReqName" });

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                            tempTable.Query,
                            builder => builder.Map(e => e.Id, f => f.Id)
                                              .Map(e => e.RequiredName, f => f.RequiredName)
                                              .Map(e => e.Name, f => f.Name!.ToUpper())
                                              .Map(e => e.Count, f => f.Count)
                                              .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
                                              .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.Name).SingleAsync()).Should().Be("LOWERCASE");
   }

   [Fact]
   public async Task Should_insert_with_static_member_of_converted_type()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Req1" });

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                            tempTable.Query,
                            builder => MapAllColumns(builder).Map(e => e.ConvertibleClass, f => _staticConvertibleClass));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.Select(e => e.ConvertibleClass).SingleAsync()).Should().Be(new ConvertibleClass(42));
      (await LoadColumnAsync<int>("TestEntities", "ConvertibleClass")).Should().Equal(42);
   }

   [Fact]
   public async Task Should_insert_with_captured_variable_of_converted_type()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "Req1" });
      var capturedValue = new ConvertibleClass(43);

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
                            tempTable.Query,
                            builder => MapAllColumns(builder).Map(e => e.ConvertibleClass, f => capturedValue));

      affectedRows.Should().Be(1);
      (await LoadColumnAsync<int>("TestEntities", "ConvertibleClass")).Should().Equal(43);
   }

   [Fact]
   public async Task Should_insert_captured_values_of_different_types()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      var id = _TEST_ENTITY_ID;
      var amount = 1234.5678m;
      var payload = """{"key": "value"}""";
      var createdAt = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Unspecified).AddTicks(1234560); // datetime(6): 123456 microseconds
      var data = new byte[] { 1, 2, 3 };

      var affectedRows = await ActDbContext.TypedEntities.BulkInsertAsync(
                            ActDbContext.QuerySources,
                            builder => builder.Map(e => e.Id, f => id)
                                              .Map(e => e.Amount, f => amount)
                                              .Map(e => e.Payload, f => payload)
                                              .Map(e => e.CreatedAt, f => createdAt)
                                              .Map(e => e.Status, f => MySqlTestStatus.Published)
                                              .Map(e => e.Data, f => data)
                                              .Map(e => e.Flag, f => true));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TypedEntities.SingleAsync();
      loadedEntity.Id.Should().Be(id);
      loadedEntity.Amount.Should().Be(amount);
      loadedEntity.Payload.Should().Be(payload);
      loadedEntity.CreatedAt.Should().Be(createdAt);
      loadedEntity.Status.Should().Be(MySqlTestStatus.Published);
      loadedEntity.Data.Should().Equal(data);
      loadedEntity.Flag.Should().BeTrue();
   }

   [Fact]
   public async Task Should_translate_server_side_functions_instead_of_evaluating_them()
   {
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 1, Name = "Source 1", Count = 1 },
                                             new MySqlQuerySource { Id = 2, Name = "Source 2", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TypedEntities.BulkInsertAsync(
                            ActDbContext.QuerySources,
                            builder => builder.Map(e => e.Id, f => Guid.NewGuid())
                                              .Map(e => e.Amount, f => 1m)
                                              .Map(e => e.CreatedAt, f => DateTime.UtcNow)
                                              .Map(e => e.Status, f => MySqlTestStatus.Draft)
                                              .Map(e => e.Flag, f => false));

      affectedRows.Should().Be(2);

      var statement = GetExecutedStatement("INSERT INTO ");
      statement.Should().Contain("UUID()");
      statement.Should().ContainAny("UTC_TIMESTAMP", "NOW(");

      var loadedEntities = await AssertDbContext.TypedEntities.OrderBy(e => e.Id).ToListAsync();
      loadedEntities.Should().HaveCount(2);
      loadedEntities.Select(e => e.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
      loadedEntities.Select(e => e.CreatedAt.Year).Should().OnlyContain(year => year == DateTime.UtcNow.Year);
   }

   [Fact]
   public async Task Should_use_sql_default_values_of_unmapped_columns()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "FromSource", Count = 7 });
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntitiesWithDefaultValues.BulkInsertAsync(
                            ActDbContext.QuerySources,
                            builder => builder.Map(e => e.NullableString, f => f.Name));

      affectedRows.Should().Be(1);

      var loadedEntity = await AssertDbContext.TestEntitiesWithDefaultValues.SingleAsync();
      loadedEntity.Id.Should().NotBe(Guid.Empty);
      loadedEntity.Int.Should().Be(1);
      loadedEntity.NullableInt.Should().Be(2);
      loadedEntity.String.Should().Be("3");
      loadedEntity.NullableString.Should().Be("FromSource");
   }

   [Fact]
   public async Task Should_let_mysql_generate_auto_increment_values()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, Name = "Name1", RequiredName = "Req1" },
                                                          new TestEntity { Id = _OTHER_TEST_ENTITY_ID, Name = "Name2", RequiredName = "Req2" });

      var affectedRows = await ActDbContext.TestEntitiesWithAutoIncrement.BulkInsertAsync(
                            tempTable.Query,
                            builder => builder.Map(e => e.Name, f => f.Name));

      affectedRows.Should().Be(2);

      var loadedEntities = await AssertDbContext.TestEntitiesWithAutoIncrement.OrderBy(e => e.Name).ToListAsync();
      loadedEntities.Select(e => e.Name).Should().Equal("Name1", "Name2");
      loadedEntities.Select(e => e.Id).Should().OnlyHaveUniqueItems().And.OnlyContain(id => id > 0);
   }

   [Fact]
   public async Task Should_insert_into_table_with_different_column_names()
   {
      ConfigureModel = ConfigureTempSourceWithMappedColumns;
      await using var tempTable = await CreateSourceAsync(new TempSourceWithMappedColumns { Id = _TEST_ENTITY_ID, Name = "Name1", Count = 1 },
                                                          new TempSourceWithMappedColumns { Id = _OTHER_TEST_ENTITY_ID, Name = null, Count = 2 });

      var affectedRows = await ActDbContext.TestEntitiesWithDifferentColumnNames.BulkInsertAsync(
                            tempTable.Query,
                            builder => builder.Map(e => e.Id, f => f.Id)
                                              .Map(e => e.Name, f => f.Name)
                                              .Map(e => e.Count, f => f.Count * 10));

      affectedRows.Should().Be(2);

      var statement = GetExecutedStatement("INSERT INTO ");
      // The alias of the temp table entity is derived from its name, so it is not part of the expectation.
      statement.Should().Contain("(`entity_id`, `display_name`, `item_count`) SELECT ");
      statement.Should().MatchRegex(@"SELECT `[^`]+`\.`entity_id`, `[^`]+`\.`display_name`, `[^`]+`\.`item_count` \* 10 FROM ");

      var loadedEntities = await AssertDbContext.TestEntitiesWithDifferentColumnNames.OrderBy(e => e.Count).ToListAsync();
      loadedEntities.Select(e => e.Id).Should().Equal(_TEST_ENTITY_ID, _OTHER_TEST_ENTITY_ID);
      loadedEntities.Select(e => e.Name).Should().Equal("Name1", null);
      loadedEntities.Select(e => e.Count).Should().Equal(10, 20);
   }

   [Fact]
   public async Task Should_throw_if_no_map_entries()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      var act = () => ActDbContext.TestEntities.BulkInsertAsync(tempTable.Query, builder => builder);

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one*");
   }

   [Fact]
   public async Task Should_throw_if_target_property_is_not_mapped()
   {
      var act = () => ActDbContext.TestEntities.BulkInsertAsync(ActDbContext.TestEntities, builder => builder.Map(e => e.Parent, f => f.Parent));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*'Parent' is not a mapped property*");
   }

   [Fact]
   public async Task Should_throw_if_target_selector_is_not_a_simple_property_access()
   {
      var act = () => ActDbContext.TestEntities.BulkInsertAsync(ActDbContext.TestEntities, builder => builder.Map(e => e.Name!.Length, f => f.Count));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*Only simple property access*");
   }

   [Fact]
   public async Task Should_throw_if_value_expression_cannot_be_translated()
   {
      var act = () => ActDbContext.TestEntities.BulkInsertAsync(ActDbContext.TestEntities, builder => builder.Map(e => e.Name, f => NotTranslatable(f.Name)));

      await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Could not translate value expression*");
   }

   [Fact]
   public async Task Should_throw_if_not_null_column_without_default_value_is_not_mapped()
   {
      await using var tempTable = await CreateSourceAsync(new TestEntity { Id = _TEST_ENTITY_ID, RequiredName = "RequiredName" });

      // 1364: field doesn't have a default value (strict mode); PropertyWithBackingField and _privateField are NOT NULL
      var exception = await ActDbContext.TestEntities
                                        .Awaiting(set => set.BulkInsertAsync(tempTable.Query,
                                                                              builder => builder.Map(e => e.Id, f => f.Id)
                                                                                                .Map(e => e.RequiredName, f => f.RequiredName)))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1364);

      (await AssertDbContext.TestEntities.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_insert_any_row_if_the_statement_fails()
   {
      ArrangeDbContext.Entities.Add(new MySqlTestEntity { Id = 1, Name = "Existing" });
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 2, Name = "New", Count = 1 },
                                             new MySqlQuerySource { Id = 3, Name = "Existing", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();

      // 1062: the name of the second source row violates the unique index of MySqlTestEntity.Name
      var exception = await ActDbContext.Entities
                                        .Awaiting(set => set.BulkInsertAsync(ActDbContext.QuerySources,
                                                                             builder => builder.Map(e => e.Id, s => s.Id)
                                                                                               .Map(e => e.Name, s => s.Name)))
                                        .Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(1062);

      (await AssertDbContext.Entities.Select(e => e.Name).ToListAsync()).Should().Equal("Existing");
   }

   [Fact]
   public async Task Should_throw_when_cancellation_is_requested()
   {
      ArrangeDbContext.QuerySources.Add(new MySqlQuerySource { Id = 1, Name = "Source", Count = 1 });
      await ArrangeDbContext.SaveChangesAsync();
      using var cancellationTokenSource = new CancellationTokenSource();
      await cancellationTokenSource.CancelAsync();

      var act = () => ActDbContext.QueryTargets.BulkInsertAsync(
                         ActDbContext.QuerySources,
                         builder => builder.Map(t => t.Id, s => s.Id)
                                           .Map(t => t.Name, s => s.Name)
                                           .Map(t => t.Count, s => s.Count),
                         cancellationToken: cancellationTokenSource.Token);

      await act.Should().ThrowAsync<OperationCanceledException>();
      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_not_end_the_transaction_of_the_caller_and_commit_rows_with_it()
   {
      await ArrangeSourcesAsync();
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await InsertTargetsAsync();

      affectedRows.Should().Be(2);
      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0, "the transaction of the caller is not committed yet");

      await transaction.CommitAsync();

      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(2);
   }

   [Fact]
   public async Task Should_not_insert_rows_if_the_transaction_of_the_caller_is_rolled_back()
   {
      await ArrangeSourcesAsync();
      await using var transaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await InsertTargetsAsync();

      affectedRows.Should().Be(2);

      await transaction.RollbackAsync();

      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_rows_into_the_nested_transaction_of_the_caller()
   {
      await ArrangeSourcesAsync();
      await using var rootTransaction = await ActDbContext.Database.BeginTransactionAsync();
      await using var nestedTransaction = await ActDbContext.Database.BeginTransactionAsync();

      var affectedRows = await InsertTargetsAsync();

      affectedRows.Should().Be(2);

      // A nested transaction is logical only; the root transaction decides about the rows.
      await nestedTransaction.CommitAsync();
      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0);

      await rootTransaction.RollbackAsync();
      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0);
   }

   [Fact]
   public async Task Should_insert_and_commit_inside_a_transaction_scope()
   {
      await ArrangeSourcesAsync();

      using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await InsertTargetsAsync();

         affectedRows.Should().Be(2);
         scope.Complete();
      }

      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(2);
   }

   [Fact]
   public async Task Should_roll_back_the_insert_if_the_transaction_scope_is_not_completed()
   {
      await ArrangeSourcesAsync();

      using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
         var affectedRows = await InsertTargetsAsync();

         affectedRows.Should().Be(2);
      }

      (await AssertDbContext.QueryTargets.CountAsync()).Should().Be(0);
   }

   private async Task ArrangeSourcesWithCurlyBracesAsync()
   {
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 1, Name = "{0}", Count = 1 },
                                             new MySqlQuerySource { Id = 2, Name = """{"key": 1}""", Count = 2 },
                                             new MySqlQuerySource { Id = 3, Name = "other", Count = 3 });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private async Task ArrangeSourcesAsync()
   {
      ArrangeDbContext.QuerySources.AddRange(new MySqlQuerySource { Id = 1, Name = "Source 1", Count = 1 },
                                             new MySqlQuerySource { Id = 2, Name = "Source 2", Count = 2 });
      await ArrangeDbContext.SaveChangesAsync();
   }

   private Task<int> InsertTargetsAsync()
   {
      return ActDbContext.QueryTargets.BulkInsertAsync(
         ActDbContext.QuerySources,
         builder => builder.Map(t => t.Id, s => s.Id)
                           .Map(t => t.Name, s => s.Name)
                           .Map(t => t.Count, s => s.Count));
   }

   private static string NotTranslatable(string? value)
   {
      return value ?? String.Empty;
   }

   private static InsertPropertyBuilder<TestEntity, TestEntity> MapAllColumns(InsertPropertyBuilder<TestEntity, TestEntity> builder)
   {
      return builder.Map(e => e.Id, f => f.Id)
                    .Map(e => e.RequiredName, f => f.RequiredName)
                    .Map(e => e.Name, f => f.Name)
                    .Map(e => e.Count, f => f.Count)
                    .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
                    .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField"));
   }

   private static void ConfigureTempSourceWithMappedColumns(ModelBuilder builder)
   {
      builder.ConfigureTempTableEntity<TempSourceWithMappedColumns>(false, b =>
                                                                           {
                                                                              b.HasKey(e => e.Id);
                                                                              b.Property(e => e.Id).HasColumnName("entity_id");
                                                                              b.Property(e => e.Name).HasColumnName("display_name");
                                                                              b.Property(e => e.Count).HasColumnName("item_count");
                                                                           });
   }

   private async Task<ITempTableQuery<T>> CreateSourceAsync<T>(params T[] entities)
      where T : class
   {
      return await SUT.BulkInsertIntoTempTableAsync(entities, new MySqlTempTableBulkInsertOptions());
   }

   // Line breaks of the generated SQL depend on the platform, so whitespace is normalized.
   private string GetExecutedStatement(string prefix)
   {
      return Regex.Replace(ExecutedCommands.Last(command => command.StartsWith(prefix, StringComparison.Ordinal)), @"\s+", " ");
   }

   private async Task<List<T>> LoadColumnAsync<T>(string tableName, string columnName)
   {
      return await AssertDbContext.Database.SqlQueryRaw<T>($"SELECT {Quote(columnName)} AS `Value` FROM {Quote(tableName)} ORDER BY {Quote(columnName)}").ToListAsync();
   }

   // LIKE copies the columns, the indexes and the primary key, but no foreign keys.
   private async Task CreateTableCopyAsync(string sourceTableName, string targetTableName)
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"CREATE TABLE {Quote(targetTableName)} LIKE {Quote(sourceTableName)}");
   }

   private async Task DropTablesAsync(params string[] tableNames)
   {
      foreach (var tableName in tableNames)
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {Quote(tableName)}");
      }
   }

   private static string Quote(string identifier)
   {
      return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
   }
}
