using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbFunctionsExtensionsTests;

// MySQL specifics: AVG() of integers is a DECIMAL, RANK() is a BIGINT UNSIGNED and COUNT() is a BIGINT.
// The window function SQL has no space between the function name and the opening parenthesis: NAME(...) OVER (...).
[Collection(MySqlTestCollection.NAME)]
public sealed class WindowFunctionTests : MySqlIntegrationTestsBase
{
   private static readonly WindowFunction<int> _averageInt = new("AVG");
   private static readonly WindowFunction<decimal> _averageDecimal = new("AVG");
   private static readonly WindowFunction<long> _rank = new("RANK");
   private static readonly WindowFunction<int> _count = new("COUNT", true);
   private static readonly WindowFunction<int?> _lag = new("LAG");

   public WindowFunctionTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_translate_function_with_1_argument_and_1_partition_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Average = EF.Functions.Average(e.Count * 2, e.Name),
                                                     OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count * 2, EF.Functions.PartitionBy(e.Name))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Average.Should().Be(5));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Average.Should().Be(8));
   }

   [Fact]
   public async Task Should_translate_function_with_1_argument_and_2_partition_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.Count, e.Name, e.RequiredName),
                                                     OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name, e.RequiredName))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.Count));
   }

   [Fact]
   public async Task Should_translate_function_with_1_argument_and_1_partition_by_and_1_order_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 4, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 5, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.Count, e.Name, EF.Functions.OrderBy(e.Count)),
                                                     OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name), EF.Functions.OrderBy(e.Count))
                                                  })
                                     .OrderBy(t => t.Count)
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
      result.Select(t => t.Average).Should().Equal(2, 3, 5);
   }

   [Fact]
   public async Task Should_translate_function_with_1_argument_and_2_partition_by_and_1_order_by()
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
                                                     Average = EF.Functions.Average(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count)),
                                                     OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name, e.RequiredName), EF.Functions.OrderBy(e.Count))
                                                  })
                                     .OrderBy(t => t.Count)
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
      result.Select(t => t.Average).Should().Equal(2, 3, 4);
   }

   [Fact]
   public async Task Should_translate_function_without_arguments_and_1_order_by()
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
                                                     Rank = EF.Functions.WindowFunction(_rank, EF.Functions.OrderBy(e.RequiredName))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Rank.Should().Be(1);
      result.Single(t => t.RequiredName == "2").Rank.Should().Be(2);
      result.Single(t => t.RequiredName == "3").Rank.Should().Be(3);
   }

   [Fact]
   public async Task Should_translate_function_without_arguments_and_1_partition_by_and_1_order_by()
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
                                                     Rank = EF.Functions.WindowFunction(_rank, EF.Functions.PartitionBy(e.Name), EF.Functions.OrderBy(e.RequiredName))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Rank.Should().Be(1);
      result.Single(t => t.RequiredName == "2").Rank.Should().Be(2);
      result.Single(t => t.RequiredName == "3").Rank.Should().Be(1);
   }

   [Fact]
   public async Task Should_translate_function_without_parameters()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     Count = EF.Functions.WindowFunction(_count)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "2").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "3").Count.Should().Be(3);
      ExecutedCommands.Should().Contain(c => c.Contains("COUNT(*) OVER (", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_translate_function_with_1_parameter()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     Count = EF.Functions.WindowFunction(_count, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "2").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "3").Count.Should().Be(3);
      ExecutedCommands.Should().Contain(c => c.Contains("COUNT(", StringComparison.Ordinal) && !c.Contains("COUNT (", StringComparison.Ordinal));
   }

   [Fact]
   public async Task Should_translate_function_with_1_parameter_and_ignore_nulls_in_count()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = null, RequiredName = "2" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.RequiredName,
                                                     Count = EF.Functions.WindowFunction(_count, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Count.Should().Be(2));
   }

   [Fact]
   public async Task Should_translate_function_with_3_partition_by_and_2_order_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2, NullableCount = 1 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "1", Count = 3, NullableCount = 1 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "1", RequiredName = "1", Count = 3, NullableCount = 2 },
                      new TestEntity { Id = new Guid("28C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "1", Count = 3, NullableCount = 1 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Rank = EF.Functions.WindowFunction(_rank, EF.Functions.PartitionBy(e.Name, e.RequiredName, e.PropertyWithBackingField % 10), EF.Functions.OrderBy(e.Count).ThenByDescending(e.NullableCount))
                                                  })
                                     .OrderBy(t => t.Id)
                                     .ToListAsync();

      // Ordered by Id: the partition (1, 1, 0) has the rows Count 3/NullableCount 2 (rank 2), Count 2 (rank 1) and Count 3/NullableCount 1 (rank 3); the partition (2, 1, 0) has a single row (rank 1).
      result.Select(t => t.Rank).Should().Equal(2, 1, 1, 3);
   }

   [Fact]
   public async Task Should_translate_function_with_arguments_partition_by_and_order_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.RequiredName,
                                                     Previous = EF.Functions.WindowFunction(_lag, e.Count, EF.Functions.PartitionBy(e.Name), EF.Functions.OrderBy(e.RequiredName))
                                                  })
                                     .OrderBy(t => t.RequiredName)
                                     .ToListAsync();

      result.Select(t => t.Previous).Should().Equal(null, 2, null);
   }

   // AVG() of integers is a DECIMAL (e.g. 2.5000). A custom window function returning int is read through the DECIMAL value.
   // Pins the behavior: the fractional part is cut off when the result is declared as int.
   [Fact]
   public async Task Should_truncate_a_fractional_average_if_the_custom_window_function_is_declared_as_int()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     AsInt = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name)),
                                                     AsDecimal = EF.Functions.WindowFunction(_averageDecimal, e.Count, EF.Functions.PartitionBy(e.Name))
                                                  })
                                     .ToListAsync();

      result.Should().AllSatisfy(t => t.AsDecimal.Should().Be(2.5m));
      result.Should().AllSatisfy(t => t.AsInt.Should().Be(2));
   }

   private async Task SeedAsync(params TestEntity[] entities)
   {
      ArrangeDbContext.TestEntities.AddRange(entities);
      await ArrangeDbContext.SaveChangesAsync();
   }
}
