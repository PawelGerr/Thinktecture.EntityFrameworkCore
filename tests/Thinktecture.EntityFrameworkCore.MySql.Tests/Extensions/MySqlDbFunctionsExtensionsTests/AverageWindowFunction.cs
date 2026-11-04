using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbFunctionsExtensionsTests;

// MySQL specifics:
// - AVG() of integers is a DECIMAL (e.g. 2.5000), of floating point values a DOUBLE and of a DECIMAL a DECIMAL with 4 additional digits of scale.
// - The CLR return type of Average<int> is int, i.e. a fractional average can't be represented. The tests with int use integral averages,
//   Should_truncate_a_fractional_average_of_int pins what happens otherwise. Use double or decimal for fractional averages.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class AverageWindowFunction : MySqlIntegrationTestsBase
{
   public AverageWindowFunction(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_return_empty_result_set_if_table_is_empty()
   {
      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Average(e.Count, e.Name))
                         .ToListAsync()).Should().BeEmpty();

      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Average(e.Count, e.Name, EF.Functions.OrderBy(e.Id)))
                         .ToListAsync()).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Average = EF.Functions.Average(e.Count * 2, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Average.Should().Be(5));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Average.Should().Be(8));
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_over_2_columns_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.Count, e.Name, e.RequiredName)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Average.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Average.Should().Be(3);
      result.Single(t => t.Name == "2").Average.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Average = EF.Functions.Average(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Average.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Average.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", NullableCount = null },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", NullableCount = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", NullableCount = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     Average = EF.Functions.Average(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Average.Should().Be(3));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Average.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_and_order_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 4, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 5, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Average.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 4).Average.Should().Be(3);
      result.Single(t => t.Name == "2").Average.Should().Be(5);
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_over_2_columns_by_and_order_by_for_not_nulls()
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
                                                     Average = EF.Functions.Average(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.RequiredName == "1").Average.Should().Be(2);
      result.Single(t => t.Name == "1" && t.RequiredName == "2").Average.Should().Be(3);
      result.Single(t => t.Name == "2").Average.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_and_order_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Average.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Average.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_and_order_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Average.Should().Be(null);
      result.Single(t => t.Name == "1" && t.Count == 2).Average.Should().Be(3);
      result.Single(t => t.Name == "2").Average.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Average_with_partition_by_and_order_by_for_mixed_nullability_different_order()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Average = EF.Functions.Average(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Average.Should().Be(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Average.Should().Be(3);
      result.Single(t => t.Name == "2").Average.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_fractional_Average_of_double_with_partition_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average = EF.Functions.Average((double)e.Count, e.Name)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Average).Should().Equal(2.5d, 2.5d, 4d);
   }

   [Fact]
   public async Task Should_generate_fractional_running_Average_of_double_with_partition_by_and_order_by()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", Count = 1, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "1", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average = EF.Functions.Average((double)e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => Math.Round(e.Average, 6)).Should().Equal(1d, 1.5d, Math.Round(7d / 3, 6));
   }

   [Fact]
   public async Task Should_generate_fractional_Average_of_decimal_column()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Amount = 1.1234m, Flag = true },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Amount = 2.0001m, Flag = true },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Amount = -0.5m, Flag = false });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TypedEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average = EF.Functions.Average(e.Amount, e.Flag),
                                                     RunningAverage = EF.Functions.Average(e.Amount, e.Flag, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Average).Should().Equal(1.56175m, 1.56175m, -0.5m);
      result.Select(e => e.RunningAverage).Should().Equal(1.1234m, 1.56175m, -0.5m);
   }

   [Fact]
   public async Task Should_generate_Average_with_1_to_5_partition_columns()
   {
      await SeedPartitionTestEntitiesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average1 = EF.Functions.Average((double)e.Count, e.Name),
                                                     Average2 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName),
                                                     Average3 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, e.NullableCount),
                                                     Average4 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField),
                                                     Average5 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // Every additional partition column splits one more row off the partition of the first rows.
      result.Select(e => e.Average1).Should().Equal(10.5, 10.5, 10.5, 10.5, 10.5, 10.5, 64);
      result.Select(e => e.Average2).Should().Equal(6.2, 6.2, 6.2, 6.2, 6.2, 32, 64);
      result.Select(e => e.Average3).Should().Equal(3.75, 3.75, 3.75, 3.75, 16, 32, 64);
      result.Select(e => Math.Round(e.Average4, 6)).Should().Equal(Math.Round(7d / 3, 6), Math.Round(7d / 3, 6), Math.Round(7d / 3, 6), 8, 16, 32, 64);
      result.Select(e => e.Average5).Should().Equal(1.5, 1.5, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Average_with_1_to_5_partition_columns_and_order_by()
   {
      await SeedPartitionTestEntitiesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average1 = EF.Functions.Average((double)e.Count, e.Name, EF.Functions.OrderBy(e.Count)),
                                                     Average2 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count)),
                                                     Average3 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, e.NullableCount, EF.Functions.OrderBy(e.Count)),
                                                     Average4 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, EF.Functions.OrderBy(e.Count)),
                                                     Average5 = EF.Functions.Average((double)e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => Math.Round(e.Average1, 6)).Should().Equal(1, 1.5, Math.Round(7d / 3, 6), 3.75, 6.2, 10.5, 64);
      result.Select(e => Math.Round(e.Average2, 6)).Should().Equal(1, 1.5, Math.Round(7d / 3, 6), 3.75, 6.2, 32, 64);
      result.Select(e => Math.Round(e.Average3, 6)).Should().Equal(1, 1.5, Math.Round(7d / 3, 6), 3.75, 16, 32, 64);
      result.Select(e => Math.Round(e.Average4, 6)).Should().Equal(1, 1.5, Math.Round(7d / 3, 6), 8, 16, 32, 64);
      result.Select(e => e.Average5).Should().Equal(1, 1.5, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_be_able_to_filter_for_fractional_Average_within_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "2", Count = 2, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average = EF.Functions.Average((double)e.Count, e.Name)
                                                  })
                                     .AsSubQuery()
                                     .Where(e => e.Average > 2.25)
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Average).Should().Equal(2.5, 2.5);
   }

   // AVG() of int values is a DECIMAL (2.5000 here) that is read as int. This pins the real behavior: the fractional part is cut off
   // (2.5 -> 2, 3.6667 -> 3), i.e. the result is truncated like the AVG() of SQL Server and not rounded.
   [Fact]
   public async Task Should_truncate_a_fractional_average_of_int()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "2", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "2", Count = 4, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000005"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Average = EF.Functions.Average(e.Count, e.Name)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Average).Should().Equal(2, 2, 3, 3, 3);
   }

   private async Task SeedAsync(params TestEntity[] entities)
   {
      ArrangeDbContext.TestEntities.AddRange(entities);
      await ArrangeDbContext.SaveChangesAsync();
   }

   // Count is a power of two. Each row differs from its predecessors in one more partition column.
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
