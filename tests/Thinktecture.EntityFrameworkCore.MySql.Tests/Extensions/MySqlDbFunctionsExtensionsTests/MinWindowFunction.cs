using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbFunctionsExtensionsTests;

// MySQL specifics:
// - MIN() returns the operand's own type, the type mapping of the operand (including its value converter) is used to read the result,
//   i.e. a value-converted column (enum stored as string, ConvertibleClass stored as int) comes back as the CLR type of the column.
// - MIN() of a value-converted column compares the stored values: strings alphabetically, not by the order of the enum members.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class MinWindowFunction : MySqlIntegrationTestsBase
{
   public MinWindowFunction(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_return_empty_result_set_if_table_is_empty()
   {
      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Min(e.Count, e.Name))
                         .ToListAsync()).Should().BeEmpty();

      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Min(e.Count, e.Name, EF.Functions.OrderBy(e.Id)))
                         .ToListAsync()).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Min = EF.Functions.Min(e.Count * 2, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Min.Should().Be(4));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Min.Should().Be(8));
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_over_2_columns_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Min = EF.Functions.Min(e.Count, e.Name, e.RequiredName)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Min.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Min.Should().Be(3);
      result.Single(t => t.Name == "2").Min.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Min = EF.Functions.Min(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Min.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Min.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", NullableCount = null },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", NullableCount = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", NullableCount = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     Min = EF.Functions.Min(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Min.Should().Be(3));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Min.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_and_order_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 4, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 5, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Min = EF.Functions.Min(e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Min.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 4).Min.Should().Be(2);
      result.Single(t => t.Name == "2").Min.Should().Be(5);
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_over_2_columns_by_and_order_by_for_not_nulls()
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
                                                     Min = EF.Functions.Min(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.RequiredName == "1").Min.Should().Be(2);
      result.Single(t => t.Name == "1" && t.RequiredName == "2").Min.Should().Be(3);
      result.Single(t => t.Name == "2").Min.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_and_order_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Min = EF.Functions.Min(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Min.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Min.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_and_order_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Min = EF.Functions.Min(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Min.Should().Be(null);
      result.Single(t => t.Name == "1" && t.Count == 2).Min.Should().Be(3);
      result.Single(t => t.Name == "2").Min.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Min_with_partition_by_and_order_by_for_mixed_nullability_different_order()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Min = EF.Functions.Min(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Min.Should().Be(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Min.Should().Be(3);
      result.Single(t => t.Name == "2").Min.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Min_with_1_to_5_partition_columns()
   {
      await SeedPartitionTestEntitiesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min1 = EF.Functions.Min(e.Count, e.Name),
                                                     Min2 = EF.Functions.Min(e.Count, e.Name, e.RequiredName),
                                                     Min3 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount),
                                                     Min4 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField),
                                                     Min5 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // Every additional partition column splits one more row off the partition of the first rows.
      result.Select(e => e.Min1).Should().Equal(1, 1, 1, 1, 1, 1, 64);
      result.Select(e => e.Min2).Should().Equal(1, 1, 1, 1, 1, 32, 64);
      result.Select(e => e.Min3).Should().Equal(1, 1, 1, 1, 16, 32, 64);
      result.Select(e => e.Min4).Should().Equal(1, 1, 1, 8, 16, 32, 64);
      result.Select(e => e.Min5).Should().Equal(1, 1, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Min_with_1_to_5_partition_columns_and_order_by()
   {
      await SeedPartitionTestEntitiesAsync();

      // Count grows with the Id. In ascending order of the Id the first row of a partition has the smallest Count,
      // so the minimum of the rows up to the current one is the minimum of the partition.
      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min1 = EF.Functions.Min(e.Count, e.Name, EF.Functions.OrderBy(e.Id)),
                                                     Min2 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Id)),
                                                     Min3 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount, EF.Functions.OrderBy(e.Id)),
                                                     Min4 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, EF.Functions.OrderBy(e.Id)),
                                                     Min5 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Min1).Should().Equal(1, 1, 1, 1, 1, 1, 64);
      result.Select(e => e.Min2).Should().Equal(1, 1, 1, 1, 1, 32, 64);
      result.Select(e => e.Min3).Should().Equal(1, 1, 1, 1, 16, 32, 64);
      result.Select(e => e.Min4).Should().Equal(1, 1, 1, 8, 16, 32, 64);
      result.Select(e => e.Min5).Should().Equal(1, 1, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_running_Min_with_order_by_in_descending_order()
   {
      await SeedPartitionTestEntitiesAsync();

      // Count grows with the Id, i.e. the minimum of the rows up to the current one is the Count of the current row.
      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min1 = EF.Functions.Min(e.Count, e.Name, EF.Functions.OrderByDescending(e.Id)),
                                                     Min5 = EF.Functions.Min(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass, EF.Functions.OrderByDescending(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Min1).Should().Equal(1, 2, 4, 8, 16, 32, 64);
      result.Select(e => e.Min5).Should().Equal(1, 2, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Min_of_strings()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", RequiredName = "gamma" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", RequiredName = "alpha" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "1", RequiredName = "beta" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "2", RequiredName = "delta" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min = EF.Functions.Min(e.RequiredName, e.Name),
                                                     RunningMin = EF.Functions.Min(e.RequiredName, e.Name, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Min).Should().Equal("alpha", "alpha", "alpha", "delta");
      result.Select(e => e.RunningMin).Should().Equal("gamma", "alpha", "alpha", "delta");
   }

   [Fact]
   public async Task Should_generate_Min_of_guids()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "1", RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "2", RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min = EF.Functions.Min(e.Id, e.Name)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Min).Should().Equal(new Guid("00000000-0000-0000-0000-000000000001"),
                                               new Guid("00000000-0000-0000-0000-000000000002"),
                                               new Guid("00000000-0000-0000-0000-000000000001"));
   }

   [Fact]
   public async Task Should_generate_Min_of_value_converted_column()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(7) },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(5) },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "2", RequiredName = "RequiredName", ConvertibleClass = null },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "3", RequiredName = "RequiredName", ConvertibleClass = new ConvertibleClass(1) });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min = EF.Functions.Min(e.ConvertibleClass, e.Name),
                                                     RunningMin = EF.Functions.Min(e.ConvertibleClass, e.Name, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Min?.Key).Should().Equal(5, 5, null, 1);
      result.Select(e => e.RunningMin?.Key).Should().Equal(7, 5, null, 1);
   }

   [Fact]
   public async Task Should_generate_Min_of_value_converted_enum()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Status = MySqlTestStatus.Published, Flag = true, CreatedAt = new DateTime(2020, 1, 1) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Status = MySqlTestStatus.Draft, Flag = true, CreatedAt = new DateTime(2020, 1, 2) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Status = MySqlTestStatus.Published, Flag = false, CreatedAt = new DateTime(2020, 1, 3) });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TypedEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Min = EF.Functions.Min(e.Status, e.Flag),
                                                     RunningMin = EF.Functions.Min(e.Status, e.Flag, EF.Functions.OrderBy(e.CreatedAt))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Min).Should().Equal(MySqlTestStatus.Draft, MySqlTestStatus.Draft, MySqlTestStatus.Published);
      result.Select(e => e.RunningMin).Should().Equal(MySqlTestStatus.Published, MySqlTestStatus.Draft, MySqlTestStatus.Published);
   }

   [Fact]
   public async Task Should_generate_Min_of_date_time_decimal_and_bool_columns()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Status = MySqlTestStatus.Draft, Amount = 1.5m, Flag = false, CreatedAt = new DateTime(2020, 1, 1, 10, 0, 0).AddTicks(1234560) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Status = MySqlTestStatus.Draft, Amount = 2.75m, Flag = true, CreatedAt = new DateTime(2021, 2, 3, 4, 5, 6).AddTicks(7654320) },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Status = MySqlTestStatus.Published, Amount = -1m, Flag = true, CreatedAt = new DateTime(2019, 1, 1) });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TypedEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     MinCreatedAt = EF.Functions.Min(e.CreatedAt, e.Status),
                                                     MinAmount = EF.Functions.Min(e.Amount, e.Status),
                                                     MinFlag = EF.Functions.Min(e.Flag, e.Status)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.MinCreatedAt).Should().Equal(new DateTime(2020, 1, 1, 10, 0, 0).AddTicks(1234560), new DateTime(2020, 1, 1, 10, 0, 0).AddTicks(1234560), new DateTime(2019, 1, 1));
      result.Select(e => e.MinAmount).Should().Equal(1.5m, 1.5m, -1m);
      result.Select(e => e.MinFlag).Should().Equal(false, false, true);
   }

   [Fact]
   public async Task Should_be_able_to_filter_for_Min_within_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Min = EF.Functions.Min(e.Count, e.Name)
                                                  })
                                     .AsSubQuery()
                                     .Where(e => e.Count == e.Min)
                                     .OrderBy(e => e.Count)
                                     .ToListAsync();

      result.Select(e => e.Count).Should().Equal(2, 4);
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
