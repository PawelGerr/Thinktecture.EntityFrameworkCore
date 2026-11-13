using MySqlConnector;
using Thinktecture.EntityFrameworkCore;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.MySqlDbFunctionsExtensionsTests;

// MySQL specifics:
// - SUM() of integers is a DECIMAL, it is read as the CLR type of the aggregated expression.
// - A window function is rendered as NAME(...) OVER (...) without a space before the parenthesis.
// - A number is not allowed as a PARTITION BY column of a window ("legacy position indication", error 3592), use columns or expressions.
// ReSharper disable once InconsistentNaming
[Collection(MySqlTestCollection.NAME)]
public sealed class SumWindowFunction : MySqlIntegrationTestsBase
{
   public SumWindowFunction(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_return_empty_result_set_if_table_is_empty()
   {
      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Sum(e.Count, e.Name))
                         .ToListAsync()).Should().BeEmpty();

      (await ActDbContext.TestEntities
                         .Select(e => EF.Functions.Sum(e.Count, e.Name, EF.Functions.OrderBy(e.Id)))
                         .ToListAsync()).Should().BeEmpty();
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Sum = EF.Functions.Sum(e.Count * 2, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(10));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(8));
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_over_2_columns_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.Count, e.Name, e.RequiredName)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     Sum = EF.Functions.Sum(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", NullableCount = null },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", NullableCount = 3 },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", NullableCount = 4 });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.RequiredName,
                                                     Sum = EF.Functions.Sum(e.NullableCount, e.Name)
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(3));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_and_order_by_for_not_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Sum.Should().Be(5);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_over_2_columns_by_and_order_by_for_not_nulls()
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
                                                     Sum = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.RequiredName == "1").Sum.Should().Be(2);
      result.Single(t => t.Name == "1" && t.RequiredName == "2").Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_and_order_by_for_nulls()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(4));
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_and_order_by_for_mixed_nullability()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Sum.Should().Be(null);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_and_order_by_for_mixed_nullability_different_order()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .ToListAsync();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Sum.Should().Be(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public async Task Should_generate_Sum_with_1_to_5_partition_columns()
   {
      await SeedPartitionTestEntitiesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Sum1 = EF.Functions.Sum(e.Count, e.Name),
                                                     Sum2 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName),
                                                     Sum3 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, e.NullableCount),
                                                     Sum4 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField),
                                                     Sum5 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // Every additional partition column splits one more row off the partition of the first rows.
      result.Select(e => e.Sum1).Should().Equal(63, 63, 63, 63, 63, 63, 64);
      result.Select(e => e.Sum2).Should().Equal(31, 31, 31, 31, 31, 32, 64);
      result.Select(e => e.Sum3).Should().Equal(15, 15, 15, 15, 16, 32, 64);
      result.Select(e => e.Sum4).Should().Equal(7, 7, 7, 8, 16, 32, 64);
      result.Select(e => e.Sum5).Should().Equal(3, 3, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Sum_with_1_to_5_partition_columns_and_order_by()
   {
      await SeedPartitionTestEntitiesAsync();

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Sum1 = EF.Functions.Sum(e.Count, e.Name, EF.Functions.OrderBy(e.Count)),
                                                     Sum2 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count)),
                                                     Sum3 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, e.NullableCount, EF.Functions.OrderBy(e.Count)),
                                                     Sum4 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, EF.Functions.OrderBy(e.Count)),
                                                     Sum5 = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, e.NullableCount, e.PropertyWithBackingField, e.ConvertibleClass, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      // The running sum of the powers of two is 2^n - 1 for the first n rows of a partition.
      result.Select(e => e.Sum1).Should().Equal(1, 3, 7, 15, 31, 63, 64);
      result.Select(e => e.Sum2).Should().Equal(1, 3, 7, 15, 31, 32, 64);
      result.Select(e => e.Sum3).Should().Equal(1, 3, 7, 15, 16, 32, 64);
      result.Select(e => e.Sum4).Should().Equal(1, 3, 7, 8, 16, 32, 64);
      result.Select(e => e.Sum5).Should().Equal(1, 3, 4, 8, 16, 32, 64);
   }

   [Fact]
   public async Task Should_generate_Sum_with_partition_by_expression()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", Count = 1, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "a", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "a", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "a", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Sum = EF.Functions.Sum(e.Count, e.Count % 2)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Sum).Should().Equal(4, 6, 4, 6);
   }

   [Fact]
   public async Task Should_generate_Sum_of_a_converted_expression_without_null_values()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", NullableCount = null, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "a", NullableCount = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "b", NullableCount = null, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Sum = EF.Functions.Sum(e.NullableCount ?? 0, e.Name)
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Sum).Should().Equal(3, 3, 0);
   }

   // SUM() is a DECIMAL in MySQL, so a long aggregate doesn't overflow the way an int aggregate would.
   [Fact]
   public async Task Should_generate_Sum_exceeding_the_range_of_int_if_declared_as_long()
   {
      await SeedAsync(new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "a", Count = Int32.MaxValue, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "a", Count = Int32.MaxValue, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => EF.Functions.Sum((long)e.Count, e.Name))
                                     .ToListAsync();

      result.Should().Equal(2L * Int32.MaxValue, 2L * Int32.MaxValue);
   }

   [Fact]
   public async Task Should_generate_Sum_of_decimal_column()
   {
      ArrangeDbContext.TypedEntities.AddRange(new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000001"), Amount = 1.1234m, Flag = true },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000002"), Amount = 2.0001m, Flag = true },
                                              new MySqlTypedEntity { Id = new Guid("00000000-0000-0000-0000-000000000003"), Amount = -0.5m, Flag = false });
      await ArrangeDbContext.SaveChangesAsync();

      var result = await ActDbContext.TypedEntities
                                     .Select(e => new
                                                  {
                                                     e.Id,
                                                     Sum = EF.Functions.Sum(e.Amount, e.Flag),
                                                     RunningSum = EF.Functions.Sum(e.Amount, e.Flag, EF.Functions.OrderBy(e.Id))
                                                  })
                                     .OrderBy(e => e.Id)
                                     .ToListAsync();

      result.Select(e => e.Sum).Should().Equal(3.1235m, 3.1235m, -0.5m);
      result.Select(e => e.RunningSum).Should().Equal(1.1234m, 3.1235m, -0.5m);
   }

   [Fact]
   public async Task Should_be_able_to_filter_for_Sum_within_subquery()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Name,
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.Count, e.Name)
                                                  })
                                     .AsSubQuery()
                                     .Where(e => e.Sum > 4)
                                     .OrderBy(e => e.Count)
                                     .ToListAsync();

      result.Select(e => e.Count).Should().Equal(2, 3);
   }

   [Fact]
   public async Task Should_generate_Sum_together_with_other_window_functions()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3, RequiredName = "RequiredName" },
                      new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4, RequiredName = "RequiredName" });

      var result = await ActDbContext.TestEntities
                                     .Select(e => new
                                                  {
                                                     e.Count,
                                                     Sum = EF.Functions.Sum(e.Count, e.Name),
                                                     Max = EF.Functions.Max(e.Count, e.Name),
                                                     RowNumber = EF.Functions.RowNumber(e.Name, EF.Functions.OrderBy(e.Count))
                                                  })
                                     .OrderBy(e => e.Count)
                                     .ToListAsync();

      result.Select(e => e.Sum).Should().Equal(5, 5, 4);
      result.Select(e => e.Max).Should().Equal(3, 3, 4);
      result.Select(e => e.RowNumber).Should().Equal(1, 2, 1);
   }

   [Fact]
   public async Task Should_render_Sum_without_a_space_before_the_parenthesis()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" });

      await ActDbContext.TestEntities
                        .Select(e => new
                                     {
                                        Partitioned = EF.Functions.Sum(e.Count, e.Name),
                                        Ordered = EF.Functions.Sum(e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                     })
                        .ToListAsync();

      var command = ExecutedCommands.Last(c => c.Contains("SUM(", StringComparison.Ordinal));
      command.Should().Contain("SUM(`t`.`Count`) OVER (PARTITION BY `t`.`Name`)");
      command.Should().Contain("SUM(`t`.`Count`) OVER (PARTITION BY `t`.`Name` ORDER BY `t`.`Count`)");
   }

   // MySQL treats a number in PARTITION BY as the position of a column and rejects it, so a numeric constant can't be used as partition column.
   [Fact]
   public async Task Should_throw_if_the_partition_column_is_a_constant()
   {
      await SeedAsync(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2, RequiredName = "RequiredName" });

      var query = ActDbContext.TestEntities.Select(e => EF.Functions.Sum(e.Count, 1));

      var exception = await query.Awaiting(q => q.ToListAsync()).Should().ThrowAsync<MySqlException>();
      exception.Which.Number.Should().Be(3592);
   }

   private async Task SeedAsync(params TestEntity[] entities)
   {
      ArrangeDbContext.TestEntities.AddRange(entities);
      await ArrangeDbContext.SaveChangesAsync();
   }

   // Count is a power of two, so the sum identifies the rows of a partition. Each row differs from its predecessors in one more partition column.
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
