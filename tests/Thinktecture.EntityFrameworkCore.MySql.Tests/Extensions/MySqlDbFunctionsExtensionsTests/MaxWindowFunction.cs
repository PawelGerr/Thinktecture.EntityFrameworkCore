using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbFunctionsExtensionsTests;

// MySQL specifics:
// - MAX() returns the operand's own type, the type mapping of the operand (including its value converter) is used to read the result,
//   i.e. a value-converted column (enum stored as string, ConvertibleClass stored as int) comes back as the CLR type of the column.
// - MAX() of a value-converted column compares the stored values: strings alphabetically, not by the order of the enum members.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class MaxWindowFunction : MySqlIntegrationTestsBase
{
   public MaxWindowFunction(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_return_empty_result_set_if_table_is_empty()
   {
      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Max(e.Count, e.Name))
                         .ToListAsync()).Should().BeEmpty();

      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Max(e.Count, e.Name, EF.Functions.OrderBy(e.Id)))
                         .ToListAsync()).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Max = EF.Functions.Max(e.Count * 2, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Max.Should().Be(6));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Max.Should().Be(8));
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_over_2_columns_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.Count, e.Name, e.RequiredName)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Max.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Max.Should().Be(3);
      result.Single(t => t.Name == "2").Max.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Max = EF.Functions.Max(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Max.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Max.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", NullableCount = null },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", NullableCount = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", NullableCount = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     Max = EF.Functions.Max(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Max.Should().Be(3));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Max.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_and_order_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 4, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 5, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Max.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 4).Max.Should().Be(4);
      result.Single(t => t.Name == "2").Max.Should().Be(5);
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_over_2_columns_by_and_order_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.RequiredName == "1").Max.Should().Be(2);
      result.Single(t => t.Name == "1" && t.RequiredName == "2").Max.Should().Be(3);
      result.Single(t => t.Name == "2").Max.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_and_order_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Max.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Max.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_and_order_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Max.Should().Be(null);
      result.Single(t => t.Name == "1" && t.Count == 2).Max.Should().Be(3);
      result.Single(t => t.Name == "2").Max.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Max_with_partition_by_and_order_by_for_mixed_nullability_different_order()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Max.Should().Be(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Max.Should().Be(3);
      result.Single(t => t.Name == "2").Max.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Max_with_1_to_5_partition_columns()
   {
      await SeedPartitionTestEntitiesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max1 = EF.Functions.Max(e.Count, e.Name),
                                                     Max2 = EF.Functions.Max(e.Count, e.Name, e.RequiredName),
                                                     Max3 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount),
                                                     Max4 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField),
                                                     Max5 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // Every additional partition column splits one more row off the partition of the first rows.
      result.Select(e => e.Max1).Should().Equal(32, 32, 32, 32, 32, 32, 64);
      result.Select(e => e.Max2).Should().Equal(16, 16, 16, 16, 16, 32, 64);
      result.Select(e => e.Max3).Should().Equal(8, 8, 8, 8, 16, 32, 64);
      result.Select(e => e.Max4).Should().Equal(4, 4, 4, 8, 16, 32, 64);
      result.Select(e => e.Max5).Should().Equal(2, 2, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Max_with_1_to_5_partition_columns_and_order_by()
   {
      await SeedPartitionTestEntitiesAsync();

      // Count grows with the Id. In descending order of the Id the first row of a partition has the largest Count,
      // so the maximum of the rows up to the current one is the maximum of the partition.
      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max1 = EF.Functions.Max(e.Count, e.Name, EF.Functions.OrderByDescending(e.Id)),
                                                     Max2 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, EF.Functions.OrderByDescending(e.Id)),
                                                     Max3 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount, EF.Functions.OrderByDescending(e.Id)),
                                                     Max4 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, EF.Functions.OrderByDescending(e.Id)),
                                                     Max5 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass, EF.Functions.OrderByDescending(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Max1).Should().Equal(32, 32, 32, 32, 32, 32, 64);
      result.Select(e => e.Max2).Should().Equal(16, 16, 16, 16, 16, 32, 64);
      result.Select(e => e.Max3).Should().Equal(8, 8, 8, 8, 16, 32, 64);
      result.Select(e => e.Max4).Should().Equal(4, 4, 4, 8, 16, 32, 64);
      result.Select(e => e.Max5).Should().Equal(2, 2, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_running_Max_with_order_by_in_ascending_order()
   {
      await SeedPartitionTestEntitiesAsync();

      // Count grows with the Id, i.e. the maximum of the rows up to the current one is the Count of the current row.
      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max1 = EF.Functions.Max(e.Count, e.Name, EF.Functions.OrderBy(e.Id)),
                                                     Max5 = EF.Functions.Max(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Max1).Should().Equal(1, 2, 4, 8, 16, 32, 64);
      result.Select(e => e.Max5).Should().Equal(1, 2, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Max_of_strings()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", RequiredName = "alpha" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", RequiredName = "gamma" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "1", RequiredName = "beta" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "2", RequiredName = "delta" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max = EF.Functions.Max(e.RequiredName, e.Name),
                                                     RunningMax = EF.Functions.Max(e.RequiredName, e.Name, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Max).Should().Equal("gamma", "gamma", "gamma", "delta");
      result.Select(e => e.RunningMax).Should().Equal("alpha", "gamma", "gamma", "delta");
   }

   [Fact]
   public async Task Should_generate_Max_of_guids()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "2", RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max = EF.Functions.Max(e.Id, e.Name)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Max).Should().Equal(new Guid("00000000-0000-0000-0000-000000000003"),
                                               new Guid("00000000-0000-0000-0000-000000000002"),
                                               new Guid("00000000-0000-0000-0000-000000000003"));
   }

   [Fact]
   public async Task Should_generate_Max_of_value_converted_column()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(5) },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(7) },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "2", RequiredName = "RequiredName", ConvertibleClass = null },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "3", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1) });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max = EF.Functions.Max(e.ConvertibleClass, e.Name),
                                                     RunningMax = EF.Functions.Max(e.ConvertibleClass, e.Name, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Max?.Key).Should().Equal(7, 7, null, 1);
      result.Select(e => e.RunningMax?.Key).Should().Equal(5, 7, null, 1);
   }

   [Fact]
   public async Task Should_generate_Max_of_value_converted_enum()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Status = MySqlTestStatus.Draft, Flag = true, CreatedAt = new DateTime(2020, 1, 1) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Status = MySqlTestStatus.Published, Flag = true, CreatedAt = new DateTime(2020, 1, 2) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Status = MySqlTestStatus.Draft, Flag = false, CreatedAt = new DateTime(2020, 1, 3) });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TypedEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Max = EF.Functions.Max(e.Status, e.Flag),
                                                     RunningMax = EF.Functions.Max(e.Status, e.Flag, EF.Functions.OrderBy(e.CreatedAt))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Max).Should().Equal(MySqlTestStatus.Published, MySqlTestStatus.Published, MySqlTestStatus.Draft);
      result.Select(e => e.RunningMax).Should().Equal(MySqlTestStatus.Draft, MySqlTestStatus.Published, MySqlTestStatus.Draft);
   }

   [Fact]
   public async Task Should_generate_Max_of_date_time_decimal_and_bool_columns()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Status = MySqlTestStatus.Draft, Amount = 1.5m, Flag = false, CreatedAt = new DateTime(2020, 1, 1, 10, 0, 0).AddTicks(1234560) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Status = MySqlTestStatus.Draft, Amount = 2.75m, Flag = true, CreatedAt = new DateTime(2021, 2, 3, 4, 5, 6).AddTicks(7654320) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Status = MySqlTestStatus.Published, Amount = -1m, Flag = false, CreatedAt = new DateTime(2019, 1, 1) });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TypedEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     MaxCreatedAt = EF.Functions.Max(e.CreatedAt, e.Status),
                                                     MaxAmount = EF.Functions.Max(e.Amount, e.Status),
                                                     MaxFlag = EF.Functions.Max(e.Flag, e.Status)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.MaxCreatedAt).Should().Equal(new DateTime(2021, 2, 3, 4, 5, 6).AddTicks(7654320), new DateTime(2021, 2, 3, 4, 5, 6).AddTicks(7654320), new DateTime(2019, 1, 1));
      result.Select(e => e.MaxAmount).Should().Equal(2.75m, 2.75m, -1m);
      result.Select(e => e.MaxFlag).Should().Equal(true, true, false);
   }

   [Fact]
   public async Task Should_be_able_to_filter_for_Max_within_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Max = EF.Functions.Max(e.Count, e.Name)
                                                  })
                                     .AsSubQuery()
                                     .Where(e => e.Count == e.Max)
                                     .OrderBy(e => e.Count)
                                     .ToListAsync();

      result.Select(e => e.Count).Should().Equal(3, 4);
   }

   private async Task SeedAsync(params TestEntity[] entities)
   {
      ArrangeDbContext.TestEntities.AddRange(entities);
      await ArrangeDbContext.SaveChangesAsync();
   }

   // Count is a power of two that grows with the Id. Each row differs from its predecessors in one more partition column.
   private async Task SeedPartitionTestEntitiesAsync()
   {
      await SeedAsync(CreatePartitionTestEntity(1, "a", "x", 1, 1, 1, 10),
                      CreatePartitionTestEntity(2, "a", "x", 2, 1, 1, 10),
                      CreatePartitionTestEntity(3, "a", "x", 4, 1, 1, 20),
                      CreatePartitionTestEntity(4, "a", "x", 8, 1, 2, 20),
                      CreatePartitionTestEntity(5, "a", "x", 16, 2, 2, 20),
                      CreatePartitionTestEntity(6, "a", "y", 32, 2, 2, 20),
                      CreatePartitionTestEntity(7, "b", "y", 64, 2, 2, 20));
   }

   private static TestEntity CreatePartitionTestEntity(int id, string name, string requiredName, int count, int nullableCount, int propertyWithBackingField, int convertibleClass)
   {
      return new TestEntity
             {
                Id = new Guid(id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                Name = name,
                RequiredName = requiredName,
                Count = count,
                NullableCount = nullableCount,
                PropertyWithBackingField = propertyWithBackingField,
                ConvertibleClass = new ConvertibleClass(convertibleClass)
             };
   }
}
