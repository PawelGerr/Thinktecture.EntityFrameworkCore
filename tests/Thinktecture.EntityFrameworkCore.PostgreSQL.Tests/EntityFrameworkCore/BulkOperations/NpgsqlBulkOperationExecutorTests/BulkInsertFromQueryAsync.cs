using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.NpgsqlBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertFromQueryAsync : IntegrationTestsBase
{
   private NpgsqlBulkOperationExecutor SUT => field ??= ActDbContext.GetService<NpgsqlBulkOperationExecutor>();

   public BulkInsertFromQueryAsync(ITestOutputHelper testOutputHelper, NpgsqlFixture npgsqlFixture)
      : base(testOutputHelper, npgsqlFixture)
   {
   }

   [Fact]
   public async Task Should_insert_entities_from_temp_table()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 10 },
            new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "Req2", Count = 20 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(2);

         var inserted = await AssertDbContext.Database
                                            .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryInsertRedirect" ORDER BY "Name" """)
                                            .ToListAsync();
         inserted.Should().HaveCount(2);
         inserted[0].Should().Be("Name1");
         inserted[1].Should().Be("Name2");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_private_field_via_ef_property()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 10 };
         entity.SetPrivateField(42);

         var sourceEntities = new List<TestEntity> { entity };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var privateFieldValues = await AssertDbContext.Database
                                                      .SqlQueryRaw<int>($"""SELECT "_privateField" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                                      .ToListAsync();
         privateFieldValues.Should().HaveCount(1);
         privateFieldValues[0].Should().Be(42);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_multiple_properties()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "TestName", RequiredName = "ReqName", Count = 42 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var counts = await AssertDbContext.Database
                                          .SqlQueryRaw<int>($"""SELECT "Count" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         counts.Should().HaveCount(1);
         counts[0].Should().Be(42);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_from_regular_dbset_query()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 99 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceQuery = ActDbContext.Set<TestEntity>()
                                       .Where(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"))
                                       .Select(e => new { e.Id, e.RequiredName, e.Name, e.Count });

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => 1)
               .Map(e => EF.Property<int>(e, "_privateField"), f => 2),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                         .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                         .ToListAsync();
         names.Should().HaveCount(1);
         names[0].Should().Be("OrigName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_generate_correct_sql_shape()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1" }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain("INSERT INTO");
         lastCommand.Should().Contain("SELECT");
         lastCommand.Should().Contain("FROM");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_entities_in_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new()
            {
               Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
               RequiredName = "RequiredName",
               Name = "InsertedName",
               Count = 99
            }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                                   .ToListAsync();
         redirectedNames.Should().HaveCount(1);
         redirectedNames[0].Should().Be("InsertedName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_throw_if_no_map_entries()
   {
      var sourceEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var act = () => ActDbContext.Set<TestEntity>().BulkInsertAsync(
         sourceQuery,
         builder => builder);

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one*");
   }

   [Fact]
   public async Task Should_return_correct_affected_row_count()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 1 },
            new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "Req2", Count = 2 },
            new() { Id = new Guid("506E664A-9ADC-4221-9577-71DCFD73DE64"), Name = "Name3", RequiredName = "Req3", Count = 3 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(3);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_constant_value()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 99 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceQuery = ActDbContext.Set<TestEntity>()
                                       .Where(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"))
                                       .Select(e => new { e.Id, e.RequiredName, e.Name });

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => 42)
               .Map(e => e.PropertyWithBackingField, f => 0)
               .Map(e => EF.Property<int>(e, "_privateField"), f => 0),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var counts = await AssertDbContext.Database
                                          .SqlQueryRaw<int>($"""SELECT "Count" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         counts.Should().HaveCount(1);
         counts[0].Should().Be(42);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_captured_variable()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 99 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceQuery = ActDbContext.Set<TestEntity>()
                                       .Where(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"))
                                       .Select(e => new { e.Id, e.RequiredName, e.Count });

         var capturedName = "CapturedValue";

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => capturedName)
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => 0)
               .Map(e => EF.Property<int>(e, "_privateField"), f => 0),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                         .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                         .ToListAsync();
         names.Should().HaveCount(1);
         names[0].Should().Be("CapturedValue");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_arithmetic_expression()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 7 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceQuery = ActDbContext.Set<TestEntity>()
                                       .Where(e => e.Id == new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"))
                                       .Select(e => new { e.Id, e.RequiredName, e.Name, e.Count });

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count * 2 + 1)
               .Map(e => e.PropertyWithBackingField, f => 0)
               .Map(e => EF.Property<int>(e, "_privateField"), f => 0),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var counts = await AssertDbContext.Database
                                          .SqlQueryRaw<int>($"""SELECT "Count" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         counts.Should().HaveCount(1);
         counts[0].Should().Be(15);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_string_to_upper()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync($"""
         CREATE TABLE "{Schema}"."TestEntities_QueryInsertRedirect" (LIKE "{Schema}"."TestEntities" INCLUDING ALL);
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "lowercase", RequiredName = "ReqName", Count = 0 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new NpgsqlTempTableBulkInsertOptions());

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            tempTable.Query,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name!.ToUpper())
               .Map(e => e.Count, f => f.Count)
               .Map(e => e.PropertyWithBackingField, f => f.PropertyWithBackingField)
               .Map(e => EF.Property<int>(e, "_privateField"), f => EF.Property<int>(f, "_privateField")),
            new NpgsqlBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect", Schema = Schema });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                          .SqlQueryRaw<string>($"""SELECT "Name" FROM "{Schema}"."TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         names.Should().HaveCount(1);
         names[0].Should().Be("LOWERCASE");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_struct_ComplexType_members_from_temp_table()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkInsertAsync(
         tempTable.Query,
         builder => builder
            .Map(e => e.Id, s => s.Id)
            .Map(e => e.MessageTimestamp, s => s.MessageTimestamp)
            .Map(e => e.EffectiveMessage.ReceivedAt, s => s.EffectiveMessage.ReceivedAt)
            .Map(e => e.EffectiveMessage.MessageTimestamp, s => s.EffectiveMessage.MessageTimestamp));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("(id, message_timestamp, effective_received_at, effective_message_timestamp)");

      var loaded = await AssertDbContext.TestEntitiesWithStructComplexType.SingleAsync();
      loaded.EffectiveMessage.Should().Be(new TimestampPair(100, 200));
   }

   [Fact]
   public async Task Should_insert_whole_struct_ComplexType_from_temp_table()
   {
      ConfigureModel = TempTableWithStructComplexType.Configure;

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([CreateStructComplexTypeSource()], new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithStructComplexType.BulkInsertAsync(
         tempTable.Query,
         builder => builder
            .Map(e => e.Id, s => s.Id)
            .Map(e => e.MessageTimestamp, s => s.MessageTimestamp)
            .Map(e => e.EffectiveMessage, s => s.EffectiveMessage));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("(id, message_timestamp, effective_message_timestamp, effective_received_at)");

      var loaded = await AssertDbContext.TestEntitiesWithStructComplexType.SingleAsync();
      loaded.EffectiveMessage.Should().Be(new TimestampPair(100, 200));
   }

   [Fact]
   public async Task Should_skip_generated_member_when_mapping_whole_ComplexType()
   {
      ConfigureModel = builder => builder.Entity<TestEntityWithComplexType>().ComplexProperty(e => e.Boundary, c => c.Property(b => b.Lower).ValueGeneratedOnAddOrUpdate());
      await ActDbContext.Database.ExecuteSqlRawAsync($"""CREATE TABLE "{Schema}"."TestEntities_with_ComplexType_Generated" (LIKE "{Schema}"."TestEntities_with_ComplexType" INCLUDING ALL); ALTER TABLE "{Schema}"."TestEntities_with_ComplexType_Generated" ALTER COLUMN "Boundary_Lower" SET DEFAULT 0;""");

      try
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""INSERT INTO "{Schema}"."TestEntities_with_ComplexType" ("Id", "Boundary_Upper", "Boundary_Lower") VALUES ('40B5CA93-5C02-48AD-B8A1-12BC13313866', 1, 2)""");

         var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkInsertAsync(
            ActDbContext.Set<TestEntityWithComplexType>(),
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.Boundary, f => f.Boundary),
            new NpgsqlBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_with_ComplexType_Generated", Schema) });

         affectedRows.Should().Be(1);
         ExecutedCommands.Last().Should().NotContain("Boundary_Lower");
         var lower = await AssertDbContext.Database.SqlQueryRaw<int>($"""SELECT "Boundary_Lower" AS "Value" FROM "{Schema}"."TestEntities_with_ComplexType_Generated" """).ToListAsync();
         lower.Should().Equal(0);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync($"""DROP TABLE IF EXISTS "{Schema}"."TestEntities_with_ComplexType_Generated" """);
      }
   }

   [Fact]
   public async Task Should_throw_for_member_of_nullable_class_ComplexType()
   {
      var act = () => ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(ActDbContext.TestEntitiesWithNullableComplexTypes, builder => builder.Map(e => e.Id, f => f.Id).Map(e => e.OptionalBoundary!.Upper, f => f.OptionalBoundary!.Upper));

      await act.Should().ThrowAsync<NotSupportedException>().WithMessage("The member 'OptionalBoundary' of '*' is a nullable complex property.*");
      ExecutedCommands.Should().NotContain(c => c.Contains("INSERT"));
   }

   [Fact]
   public async Task Should_throw_when_every_member_of_whole_ComplexType_is_generated_and_another_column_is_mapped()
   {
      ConfigureModel = builder => builder.Entity<TestEntityWithComplexType>().ComplexProperty(e => e.Boundary, c =>
                                                                                              {
                                                                                                 c.Property(b => b.Upper).ValueGeneratedOnAddOrUpdate();
                                                                                                 c.Property(b => b.Lower).ValueGeneratedOnAddOrUpdate();
                                                                                              });

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkInsertAsync(
         ActDbContext.Set<TestEntityWithComplexType>(),
         builder => builder
            .Map(e => e.Id, f => f.Id)
            .Map(e => e.Boundary, f => f.Boundary));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*every member*generated*Expression: e => e.Boundary");
      ExecutedCommands.Should().NotContain(c => c.Contains("INSERT"));
   }

   [Fact]
   public async Task Should_map_whole_nullable_ComplexTypes_from_source_columns()
   {
      var sourceRows = new[]
                       {
                          TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("AB6D3F7E-4C9C-4DAF-9B79-BF6EAF5C0001")),
                          TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("AB6D3F7E-4C9C-4DAF-9B79-BF6EAF5C0002"))
                       };
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceRows, new NpgsqlTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         tempTable.Query,
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.OptionalBoundary, s => s.OptionalBoundary)
                           .Map(e => e.OptionalPair, s => s.OptionalPair)
                           .Map(e => e.Tagged, s => s.Tagged)
                           .Map(e => e.Outer, s => s.Outer)
                           .Map(e => e.OptionalOuter, s => s.OptionalOuter)
                           .Map(e => e.OptionalConverted, s => s.OptionalConverted));

      affectedRows.Should().Be(2);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.ToListAsync()).Should().BeEquivalentTo(sourceRows);
   }

   [Fact]
   public async Task Should_map_whole_nullable_ComplexTypes_from_captured_values_and_nulls()
   {
      ArrangeDbContext.Add(new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" });
      await ArrangeDbContext.SaveChangesAsync();

      var id = new Guid("AB6D3F7E-4C9C-4DAF-9B79-BF6EAF5C0004");
      var boundary = new BoundaryValueObject(2, 5);
      TimestampPair? pair = null;
      var tagged = new TaggedValues { X = 7, Text = "t" };
      var outer = new OuterWithOptionalInner { Z = 1, Inner = null };
      OptionalOuter? optionalOuter = null;
      ConvertedTimestampPair? converted = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2));

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntities,
         builder => builder.Map(e => e.Id, s => id)
                           .Map(e => e.OptionalBoundary, s => boundary)
                           .Map(e => e.OptionalPair, s => pair)
                           .Map(e => e.Tagged, s => tagged)
                           .Map(e => e.Outer, s => outer)
                           .Map(e => e.OptionalOuter, s => optionalOuter)
                           .Map(e => e.OptionalConverted, s => converted));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync()).Should().BeEquivalentTo(
         new TestEntityWithNullableComplexTypes
         {
            Id = id,
            OptionalBoundary = new BoundaryValueObject(2, 5),
            OptionalPair = null,
            Tagged = new TaggedValues { X = 7, Text = "t" },
            Outer = new OuterWithOptionalInner { Z = 1, Inner = null },
            OptionalOuter = null,
            OptionalConverted = new ConvertedTimestampPair(new TimestampValue(1), new TimestampValue(2))
         });
   }

   private static TempTableWithStructComplexType CreateStructComplexTypeSource()
   {
      return new TempTableWithStructComplexType
             {
                Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"),
                MessageTimestamp = 10,
                EffectiveMessage = new TimestampPair(100, 200)
             };
   }

   [Fact]
   public async Task Should_insert_rows_When_source_query_compares_column_with_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0020"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0021"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntities.Where(s => s.Name == null),
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.Outer.Z, s => s.Name == null ? 1 : 2));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
      var inserted = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      inserted.Id.Should().Be(nullName.Id);
      inserted.Outer.Z.Should().Be(1);
   }

   [Fact]
   public async Task Should_insert_rows_When_source_query_compares_column_with_not_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0022"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0023"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntities.Where(s => s.Name != null),
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.Outer.Z, s => 1));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("IS NOT NULL").And.NotContain("<> NULL");
      var inserted = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      inserted.Id.Should().Be(withName.Id);
      inserted.Outer.Z.Should().Be(1);
   }

   [Fact]
   public async Task Should_insert_rows_When_source_query_compares_column_with_captured_null()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0024"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0025"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      string? captured = null;

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntities.Where(s => s.Name == captured),
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.Outer.Z, s => 1));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
      var inserted = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      inserted.Id.Should().Be(nullName.Id);
      inserted.Outer.Z.Should().Be(1);
   }

   [Fact]
   public async Task Should_insert_rows_When_source_query_uses_collection_parameter()
   {
      var nullName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0026"), Name = null, RequiredName = "RequiredName" };
      var withName = new TestEntity { Id = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0027"), Name = "x", RequiredName = "RequiredName" };
      ArrangeDbContext.AddRange(nullName, withName);
      await ArrangeDbContext.SaveChangesAsync();

      var ids = new List<Guid> { nullName.Id };

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntities.Where(s => ids.Contains(s.Id)),
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.Outer.Z, s => 1));

      affectedRows.Should().Be(1);
      var inserted = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync();
      inserted.Id.Should().Be(nullName.Id);
      inserted.Outer.Z.Should().Be(1);
   }

   [Fact]
   public async Task Should_insert_rows_When_source_query_filters_nullable_ComplexType()
   {
      var withValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0030"));
      var withNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0031"));
      ArrangeDbContext.AddRange(withValues, withNulls);
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("5D1C7A3E-0F2B-4E6A-9C1D-2B7F3E4A0032");

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntitiesWithNullableComplexTypes.Where(s => s.OptionalBoundary != null),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.OptionalBoundary, s => s.OptionalBoundary)
                           .Map(e => e.Outer, s => s.Outer));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("IS NOT NULL");
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == newId)).OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(2, 5));
   }

   [Fact]
   public async Task Should_map_whole_nullable_ComplexType_from_coalesce_with_column_left_operand()
   {
      var sourceWithNulls = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("AB6D3F7E-4C9C-4DAF-9B79-BF6EAF5C0020"));
      var sourceWithValues = TestEntityWithNullableComplexTypes.CreateWithValues(new Guid("AB6D3F7E-4C9C-4DAF-9B79-BF6EAF5C0021"));
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([sourceWithNulls, sourceWithValues], new NpgsqlTempTableBulkInsertOptions());

      var boundary = new BoundaryValueObject(3, 9);

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         tempTable.Query,
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.Outer, s => s.Outer)
                           .Map(e => e.OptionalBoundary, s => s.OptionalBoundary ?? boundary));

      affectedRows.Should().Be(2);
      ExecutedCommands.Last().Should().Contain("IS NULL").And.NotContain("= NULL");
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == sourceWithNulls.Id)).OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(3, 9));
      (await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == sourceWithValues.Id)).OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(2, 5));
   }

   [Fact]
   public async Task Should_insert_value_converted_captured_value()
   {
      var source = new TestEntity { Id = new Guid("7A000000-0000-0000-0000-000000000005"), RequiredName = "RequiredName" };
      ArrangeDbContext.Add(source);
      await ArrangeDbContext.SaveChangesAsync();

      var captured = new ConvertibleClass(7);
      var newId = new Guid("7A000000-0000-0000-0000-000000000006");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(s => s.Id == source.Id),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.Count)
                           .Map(e => e.PropertyWithBackingField, s => s.PropertyWithBackingField)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => EF.Property<int>(s, "_privateField"))
                           .Map(e => e.ConvertibleClass, s => captured));

      affectedRows.Should().Be(1);
      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId)).ConvertibleClass!.Key.Should().Be(7);
   }

   [Fact]
   public async Task Should_insert_When_same_source_column_is_mapped_to_several_target_columns()
   {
      var source = new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000001"), RequiredName = "Req", Count = 7 };
      ArrangeDbContext.Add(source);
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("7B000000-0000-0000-0000-000000000002");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(e => e.Id == source.Id),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.Count)
                           .Map(e => e.PropertyWithBackingField, s => s.Count)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => s.Count));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("t.\"Count\", t.\"Count\", t.\"Count\"");

      var inserted = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId);
      inserted.RequiredName.Should().Be("Req");
      inserted.Count.Should().Be(7);
      inserted.PropertyWithBackingField.Should().Be(7);
      inserted.GetPrivateField().Should().Be(7);
   }

   [Fact]
   public async Task Should_insert_When_same_arithmetic_expression_is_mapped_to_several_target_columns()
   {
      var source = new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000003"), RequiredName = "Req", Count = 7 };
      ArrangeDbContext.Add(source);
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("7B000000-0000-0000-0000-000000000004");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(e => e.Id == source.Id),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.Count * 2 + 1)
                           .Map(e => e.PropertyWithBackingField, s => s.Count * 2 + 1)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => s.Count));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("t.\"Count\" * 2 + 1, t.\"Count\" * 2 + 1");

      var inserted = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId);
      inserted.Count.Should().Be(15);
      inserted.PropertyWithBackingField.Should().Be(15);
      inserted.GetPrivateField().Should().Be(7);
   }

   [Fact]
   public async Task Should_map_whole_nullable_ComplexType_from_server_conditional_with_equal_member_values()
   {
      var sourceWithPositiveCount = new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000005"), RequiredName = "Req", Count = 1 };
      var sourceWithZeroCount = new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000006"), RequiredName = "Req", Count = 0 };
      ArrangeDbContext.AddRange(sourceWithPositiveCount, sourceWithZeroCount);
      await ArrangeDbContext.SaveChangesAsync();

      var boundary = new BoundaryValueObject(3, 3);
      var outer = new OuterWithOptionalInner { Z = 1, Inner = null };

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntities.Where(e => e.Id == sourceWithPositiveCount.Id || e.Id == sourceWithZeroCount.Id),
         builder => builder.Map(e => e.Id, s => s.Id)
                           .Map(e => e.Outer, s => outer)
                           .Map(e => e.OptionalBoundary, s => s.Count > 0 ? boundary : null));

      affectedRows.Should().Be(2);
      System.Text.RegularExpressions.Regex.Matches(ExecutedCommands.Last(), @"CASE\s+WHEN").Count.Should().Be(2);

      var withBoundary = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == sourceWithPositiveCount.Id);
      withBoundary.OptionalBoundary.Should().BeEquivalentTo(new BoundaryValueObject(3, 3));
      withBoundary.Outer.Z.Should().Be(1);
      withBoundary.Outer.Inner.Should().BeNull();

      var withoutBoundary = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == sourceWithZeroCount.Id);
      withoutBoundary.OptionalBoundary.Should().BeNull();
      withoutBoundary.Outer.Z.Should().Be(1);
      withoutBoundary.Outer.Inner.Should().BeNull();
   }

   [Fact]
   public async Task Should_insert_When_same_source_column_is_mapped_twice_and_source_is_ordered_and_paged()
   {
      ArrangeDbContext.AddRange(
         new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000007"), RequiredName = "Req", Count = 1 },
         new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000008"), RequiredName = "Req", Count = 2 },
         new TestEntity { Id = new Guid("7B000000-0000-0000-0000-000000000009"), RequiredName = "Req", Count = 3 });
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("7B000000-0000-0000-0000-00000000000A");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.OrderBy(e => e.Count).Skip(1).Take(1),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.Count)
                           .Map(e => e.PropertyWithBackingField, s => s.Count)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => s.Count));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("t.\"Count\", t.\"Count\", t.\"Count\"").And.Contain("ORDER BY").And.Contain("LIMIT @").And.Contain("OFFSET @");

      var inserted = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId);
      inserted.Count.Should().Be(2);
      inserted.PropertyWithBackingField.Should().Be(2);
      inserted.GetPrivateField().Should().Be(2);
   }

   [Fact]
   public async Task Should_insert_When_same_source_column_is_mapped_twice_and_source_is_distinct_projection()
   {
      ArrangeDbContext.AddRange(
         new TestEntity { Id = new Guid("7B000000-0000-0000-0000-00000000000B"), RequiredName = "Req", Count = 4 },
         new TestEntity { Id = new Guid("7B000000-0000-0000-0000-00000000000C"), RequiredName = "Req", Count = 4 });
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("7B000000-0000-0000-0000-00000000000D");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Select(e => new { e.RequiredName, e.Count }).Distinct(),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.Count)
                           .Map(e => e.PropertyWithBackingField, s => s.Count)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => s.Count));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("SELECT DISTINCT").And.Contain("t.\"Count\", t.\"Count\", t.\"Count\"");

      var inserted = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId);
      inserted.Count.Should().Be(4);
      inserted.PropertyWithBackingField.Should().Be(4);
      inserted.GetPrivateField().Should().Be(4);
   }

   [Fact]
   public async Task Should_insert_When_same_source_column_and_equal_constants_are_mapped_to_several_target_columns()
   {
      var source = new TestEntity { Id = new Guid("7B000000-0000-0000-0000-00000000000E"), RequiredName = "Req", Count = 7 };
      ArrangeDbContext.Add(source);
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("7B000000-0000-0000-0000-00000000000F");

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(e => e.Id == source.Id),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => s.Count)
                           .Map(e => e.PropertyWithBackingField, s => s.Count)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => 5)
                           .Map(e => e.NullableCount, s => 5));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("t.\"Count\", t.\"Count\", @__insert_const_1, @__insert_const_2");

      var inserted = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId);
      inserted.Count.Should().Be(7);
      inserted.PropertyWithBackingField.Should().Be(7);
      inserted.GetPrivateField().Should().Be(5);
      inserted.NullableCount.Should().Be(5);
   }

   [Fact]
   public async Task Should_insert_When_value_is_count_of_captured_collection_filtered_with_lambda()
   {
      var sourceId = new Guid("8B000000-0000-0000-0000-0000000000B0");
      var newId = new Guid("8B000000-0000-0000-0000-0000000000B1");
      ArrangeDbContext.Add(new TestEntity { Id = sourceId, RequiredName = "Req", Count = 7 });
      await ArrangeDbContext.SaveChangesAsync();

      var items = new List<CapturedItem> { new(Guid.NewGuid(), true), new(Guid.NewGuid(), false), new(Guid.NewGuid(), true) };

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(e => e.Id == sourceId),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => items.Where(i => i.Active).Count())
                           .Map(e => e.PropertyWithBackingField, s => s.Count)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => s.Count));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("__insert_const_1");

      var inserted = await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId);
      inserted.Count.Should().Be(2);
      inserted.PropertyWithBackingField.Should().Be(7);
   }

   [Fact]
   public async Task Should_map_whole_nullable_ComplexTypes_from_captured_collection_with_lambda()
   {
      var source = TestEntityWithNullableComplexTypes.CreateWithNulls(new Guid("7C000000-0000-0000-0000-00000000000D"));
      ArrangeDbContext.Add(source);
      await ArrangeDbContext.SaveChangesAsync();

      var newId = new Guid("7C000000-0000-0000-0000-00000000000E");
      var boundaries = new List<BoundaryValueObject> { new(1, 2) };
      var tagged = new List<TaggedValues> { new() { X = 8, Text = "u" } };

      var affectedRows = await ActDbContext.TestEntitiesWithNullableComplexTypes.BulkInsertAsync(
         ActDbContext.TestEntitiesWithNullableComplexTypes,
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.Outer, s => s.Outer)
                           .Map(e => e.OptionalBoundary, s => boundaries.FirstOrDefault(b => b.Upper == 99))
                           .Map(e => e.Tagged, s => tagged.FirstOrDefault(t => t.X == 8)));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntitiesWithNullableComplexTypes.SingleAsync(e => e.Id == newId);
      loaded.OptionalBoundary.Should().BeNull();
      loaded.Tagged.Should().BeEquivalentTo(new TaggedValues { X = 8, Text = "u" });
   }

   [Fact]
   public async Task Should_insert_When_value_uses_captured_collection_with_lambda_in_server_conditional()
   {
      var sourceId = new Guid("8B000000-0000-0000-0000-0000000000B2");
      var newId = new Guid("8B000000-0000-0000-0000-0000000000B3");
      ArrangeDbContext.Add(new TestEntity { Id = sourceId, RequiredName = "Req", Count = 7 });
      await ArrangeDbContext.SaveChangesAsync();

      var items = new List<CapturedItem> { new(sourceId, true), new(Guid.NewGuid(), false) };

      var affectedRows = await ActDbContext.TestEntities.BulkInsertAsync(
         ActDbContext.TestEntities.Where(e => e.Id == sourceId),
         builder => builder.Map(e => e.Id, s => newId)
                           .Map(e => e.RequiredName, s => s.RequiredName)
                           .Map(e => e.Count, s => items.Select(i => i.Id).Contains(s.Id) ? 1 : 0)
                           .Map(e => e.PropertyWithBackingField, s => s.Count)
                           .Map(e => EF.Property<int>(e, "_privateField"), s => s.Count));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("CASE");
      ExecutedCommands.Last().Should().Contain(" IN (");

      (await AssertDbContext.TestEntities.SingleAsync(e => e.Id == newId)).Count.Should().Be(1);
   }

   private sealed record CapturedItem(Guid Id, bool Active);
}
