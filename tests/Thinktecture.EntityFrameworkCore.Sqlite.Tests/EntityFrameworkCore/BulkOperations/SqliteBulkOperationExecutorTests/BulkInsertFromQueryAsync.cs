using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.TestDatabaseContext;

// ReSharper disable InconsistentNaming

#pragma warning disable CS0618 // TableName and Schema are obsolete; these tests cover the obsolete path on purpose.

namespace Thinktecture.EntityFrameworkCore.BulkOperations.SqliteBulkOperationExecutorTests;

// ReSharper disable once InconsistentNaming
public class BulkInsertFromQueryAsync : IntegrationTestsBase
{
   private SqliteBulkOperationExecutor SUT => field ??= ActDbContext.GetService<SqliteBulkOperationExecutor>();

   public BulkInsertFromQueryAsync(ITestOutputHelper testOutputHelper, DbContextProviderFactoryFixture providerFactoryFixture)
      : base(testOutputHelper, providerFactoryFixture)
   {
   }

   [Fact]
   public async Task Should_insert_entities_from_temp_table()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 10 },
            new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "Req2", Count = 20 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(2);

         var inserted = await AssertDbContext.Database
                                            .SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_QueryInsertRedirect" ORDER BY "Name" """)
                                            .ToListAsync();
         inserted.Should().HaveCount(2);
         inserted[0].Should().Be("Name1");
         inserted[1].Should().Be("Name2");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   private static readonly ConvertibleClass _staticConvertibleClass = new(42);

   [Fact]
   public async Task Should_insert_static_member_of_converted_type()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "ConvertibleClass" INTEGER NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "Req1" }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.ConvertibleClass, f => _staticConvertibleClass),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var inserted = await AssertDbContext.Database
                                             .SqlQueryRaw<int>("""SELECT "ConvertibleClass" FROM "TestEntities_QueryInsertRedirect" """)
                                             .ToListAsync();
         inserted.Should().BeEquivalentTo([42]);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_private_field_via_ef_property()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            "PropertyWithBackingField" INTEGER NOT NULL DEFAULT 0,
            "_privateField" INTEGER NOT NULL DEFAULT 0,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 10 };
         entity.SetPrivateField(42);

         var sourceEntities = new List<TestEntity> { entity };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

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
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var privateFieldValues = await AssertDbContext.Database
                                                      .SqlQueryRaw<int>("""SELECT "_privateField" FROM "TestEntities_QueryInsertRedirect" """)
                                                      .ToListAsync();
         privateFieldValues.Should().HaveCount(1);
         privateFieldValues[0].Should().Be(42);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_multiple_properties()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "TestName", RequiredName = "ReqName", Count = 42 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var counts = await AssertDbContext.Database
                                          .SqlQueryRaw<int>("""SELECT "Count" FROM "TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         counts.Should().HaveCount(1);
         counts[0].Should().Be(42);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_from_regular_dbset_query()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 99 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
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
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                         .SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_QueryInsertRedirect" """)
                                         .ToListAsync();
         names.Should().HaveCount(1);
         names[0].Should().Be("OrigName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_generate_correct_sql_shape()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1" }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         var lastCommand = ExecutedCommands.Last();
         lastCommand.Should().Contain("INSERT INTO");
         lastCommand.Should().Contain("SELECT");
         lastCommand.Should().Contain("FROM");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_entities_in_table_name_override()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
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

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var redirectedNames = await AssertDbContext.Database
                                                   .SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_QueryInsertRedirect" """)
                                                   .ToListAsync();
         redirectedNames.Should().HaveCount(1);
         redirectedNames[0].Should().Be("InsertedName");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_throw_if_no_map_entries()
   {
      var sourceEntities = new List<TestEntity>
      {
         new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }
      };

      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

      var sourceQuery = tempTable.Query;

      var act = () => ActDbContext.Set<TestEntity>().BulkInsertAsync(
         sourceQuery,
         builder => builder);

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one*");
   }

   [Fact]
   public async Task Should_throw_when_target_column_is_mapped_twice()
   {
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), RequiredName = "RequiredName" }], new SqliteTempTableBulkInsertOptions());

      var act = () => ActDbContext.Set<TestEntity>().BulkInsertAsync(tempTable.Query, builder => builder.Map(e => e.Id, f => f.Id).Map(e => e.RequiredName, f => f.RequiredName).Map(e => e.Id, f => f.Id));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("The target column 'Id' is mapped more than once.*");
   }

   [Fact]
   public async Task Should_return_correct_affected_row_count()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 1 },
            new() { Id = new Guid("8AF163D7-D316-4B2D-A62F-6326A80C8BEE"), Name = "Name2", RequiredName = "Req2", Count = 2 },
            new() { Id = new Guid("506E664A-9ADC-4221-9577-71DCFD73DE64"), Name = "Name3", RequiredName = "Req3", Count = 3 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var sourceQuery = tempTable.Query;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            sourceQuery,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name)
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(3);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_constant_value()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 99 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
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
               .Map(e => e.Count, f => 42),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var counts = await AssertDbContext.Database
                                          .SqlQueryRaw<int>("""SELECT "Count" FROM "TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         counts.Should().HaveCount(1);
         counts[0].Should().Be(42);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_captured_variable()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 99 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
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
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                         .SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_QueryInsertRedirect" """)
                                         .ToListAsync();
         names.Should().HaveCount(1);
         names[0].Should().Be("CapturedValue");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_captured_null_variable()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "Name1", RequiredName = "Req1", Count = 10 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         string? name = null;

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            tempTable.Query,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => name)
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableNameProvider = IBulkOperationTableNameProvider.ForRootEntity("TestEntities_QueryInsertRedirect") });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                         .SqlQueryRaw<string?>("""SELECT "Name" AS "Value" FROM "TestEntities_QueryInsertRedirect" """)
                                         .ToListAsync();
         names.Should().ContainSingle().Which.Should().BeNull();
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_arithmetic_expression()
   {
      var entity = new TestEntity { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "OrigName", RequiredName = "ReqName", Count = 7 };
      ArrangeDbContext.Add(entity);
      await ArrangeDbContext.SaveChangesAsync();

      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
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
               .Map(e => e.Count, f => f.Count * 2 + 1),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var counts = await AssertDbContext.Database
                                          .SqlQueryRaw<int>("""SELECT "Count" FROM "TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         counts.Should().HaveCount(1);
         counts[0].Should().Be(15);
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_insert_with_string_to_upper()
   {
      await ActDbContext.Database.ExecuteSqlRawAsync("""
         CREATE TABLE "TestEntities_QueryInsertRedirect" (
            "Id" TEXT NOT NULL,
            "RequiredName" TEXT NOT NULL,
            "Name" TEXT NULL,
            "Count" INTEGER NOT NULL,
            CONSTRAINT "PK_TestEntities_QueryInsertRedirect" PRIMARY KEY ("Id")
         );
         """);

      try
      {
         var sourceEntities = new List<TestEntity>
         {
            new() { Id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), Name = "lowercase", RequiredName = "ReqName", Count = 0 }
         };

         await using var tempTable = await SUT.BulkInsertIntoTempTableAsync(sourceEntities, new SqliteTempTableBulkInsertOptions());

         var affectedRows = await ActDbContext.Set<TestEntity>().BulkInsertAsync(
            tempTable.Query,
            builder => builder
               .Map(e => e.Id, f => f.Id)
               .Map(e => e.RequiredName, f => f.RequiredName)
               .Map(e => e.Name, f => f.Name!.ToUpper())
               .Map(e => e.Count, f => f.Count),
            new SqliteBulkInsertFromQueryOptions { TableName = "TestEntities_QueryInsertRedirect" });

         affectedRows.Should().Be(1);

         var names = await AssertDbContext.Database
                                          .SqlQueryRaw<string>("""SELECT "Name" FROM "TestEntities_QueryInsertRedirect" """)
                                          .ToListAsync();
         names.Should().HaveCount(1);
         names[0].Should().Be("LOWERCASE");
      }
      finally
      {
         await ActDbContext.Database.ExecuteSqlRawAsync("""DROP TABLE IF EXISTS "TestEntities_QueryInsertRedirect" """);
      }
   }

   [Fact]
   public async Task Should_throw_when_captured_whole_ComplexType_value_is_null()
   {
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866"), new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());
      BoundaryValueObject? value = null;

      var act = () => ActDbContext.Set<TestEntityWithComplexType>().BulkInsertAsync(tempTable.Query, builder => builder.Map(e => e.Id, f => f.Id).Map(e => e.Boundary, f => value!));

      await act.Should().ThrowAsync<ArgumentException>().WithMessage("*cannot be read because the captured value is null.*");
   }

   [Fact]
   public async Task Should_insert_ComplexType_members_from_temp_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkInsertAsync(
         tempTable.Query,
         builder => builder
            .Map(e => e.Id, f => f.Id)
            .Map(e => e.Boundary.Upper, f => f.Boundary.Upper)
            .Map(e => e.Boundary.Lower, f => f.Boundary.Lower));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("(\"Id\", \"Boundary_Upper\", \"Boundary_Lower\")");

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(10);
      loaded.Boundary.Lower.Should().Be(20);
   }

   [Fact]
   public async Task Should_insert_whole_ComplexType_from_captured_variable()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var value = new BoundaryValueObject(7, 8);

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkInsertAsync(
         tempTable.Query,
         builder => builder
            .Map(e => e.Id, f => f.Id)
            .Map(e => e.Boundary, f => value));

      affectedRows.Should().Be(1);

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(7);
      loaded.Boundary.Lower.Should().Be(8);
   }

   [Fact]
   public async Task Should_insert_whole_ComplexType_from_temp_table()
   {
      var id = new Guid("40B5CA93-5C02-48AD-B8A1-12BC13313866");
      await using var tempTable = await SUT.BulkInsertIntoTempTableAsync([new TestEntityWithComplexType(id, new BoundaryValueObject(10, 20))], new SqliteTempTableBulkInsertOptions());

      var affectedRows = await ActDbContext.Set<TestEntityWithComplexType>().BulkInsertAsync(
         tempTable.Query,
         builder => builder
            .Map(e => e.Id, f => f.Id)
            .Map(e => e.Boundary, f => f.Boundary));

      affectedRows.Should().Be(1);
      ExecutedCommands.Last().Should().Contain("(\"Id\", \"Boundary_Lower\", \"Boundary_Upper\")");

      var loaded = await AssertDbContext.TestEntities_with_ComplexType.SingleAsync();
      loaded.Boundary.Upper.Should().Be(10);
      loaded.Boundary.Lower.Should().Be(20);
   }
}
