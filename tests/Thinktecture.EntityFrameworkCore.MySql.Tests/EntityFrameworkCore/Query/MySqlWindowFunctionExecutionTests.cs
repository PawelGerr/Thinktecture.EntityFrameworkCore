namespace Thinktecture.EntityFrameworkCore.Query;

[Collection(MySqlTestCollection.NAME)]
public sealed class MySqlWindowFunctionExecutionTests : MySqlIntegrationTestsBase
{
   private static readonly WindowFunction<long> _countAll = new("COUNT", useAsteriskWhenNoArguments: true);
   private static readonly WindowFunction<long> _sum = new("SUM");
   private static readonly WindowFunction<int> _max = new("MAX");
   private static readonly WindowFunction<long> _bitOr = new("BIT_OR");
   private static readonly WindowFunction<long> _rank = new("RANK");
   private static readonly WindowFunction<int?> _lead = new("LEAD");

   public MySqlWindowFunctionExecutionTests(MySqlContainerFixture fixture)
      : base(fixture)
   {
   }

   [Fact]
   public async Task Should_execute_count_star()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_countAll))
                                .ToListAsync();

      result.Should().Equal(3, 3, 3);
   }

   [Fact]
   public async Task Should_execute_sum()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_sum, entity.Id))
                                .ToListAsync();

      result.Should().Equal(7, 7, 7);
   }

   [Fact]
   public async Task Should_execute_max()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_max, entity.Id))
                                .ToListAsync();

      result.Should().Equal(4, 4, 4);
   }

   [Fact]
   public async Task Should_execute_bit_or()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_bitOr, entity.Id))
                                .ToListAsync();

      result.Should().Equal(7, 7, 7);
   }

   [Fact]
   public async Task Should_execute_rank_with_order_by()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_rank, EF.Functions.OrderBy(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(1, 2, 3);
   }

   [Fact]
   public async Task Should_execute_row_number()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.RowNumber(EF.Functions.OrderByDescending(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(3, 2, 1);
   }

   [Fact]
   public async Task Should_execute_ntile()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.NTile(2, EF.Functions.OrderBy(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(1, 1, 2);
   }

   [Fact]
   public async Task Should_execute_lead_with_multiple_arguments()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_lead, entity.Id, 1, EF.Functions.OrderBy(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(2, 4, null);
   }

   [Fact]
   public async Task Should_execute_partitioned_count()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_countAll, EF.Functions.PartitionBy(entity.Name)))
                                .ToListAsync();

      result.Should().Equal(1, 1, 1);
   }

   [Fact]
   public async Task Should_execute_sum_average_max_and_min_window_functions()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => new
                                                 {
                                                    entity.Id,
                                                    Sum = EF.Functions.Sum(entity.Id, entity.Id % 2),
                                                    Average = EF.Functions.Average(entity.Id, entity.Id % 2),
                                                    Max = EF.Functions.Max(entity.Id, entity.Id % 2),
                                                    Min = EF.Functions.Min(entity.Id, entity.Id % 2)
                                                 })
                                .ToListAsync();

      result.Select(row => row.Sum).Should().Equal(1, 6, 6);
      result.Select(row => row.Average).Should().Equal(1, 3, 3);
      result.Select(row => row.Max).Should().Equal(1, 4, 4);
      result.Select(row => row.Min).Should().Equal(1, 2, 2);
   }

   [Fact]
   public async Task Should_execute_ordered_sum_window_function()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.Sum(entity.Id, entity.Id % 2, EF.Functions.OrderBy(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(1, 2, 6);
   }

   [Fact]
   public async Task Should_execute_aggregate_window_functions_with_five_partition_columns()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => new
                                                 {
                                                    Max = EF.Functions.Max(entity.Id,
                                                                           entity.Id % 2,
                                                                           entity.Id % 3,
                                                                           entity.Name,
                                                                           entity.Id > 0,
                                                                           entity.Id + 1,
                                                                           EF.Functions.OrderBy(entity.Id)),
                                                    Min = EF.Functions.Min(entity.Id,
                                                                           entity.Id % 2,
                                                                           entity.Id % 3,
                                                                           entity.Name,
                                                                           entity.Id > 0,
                                                                           entity.Id + 1)
                                                 })
                                .ToListAsync();

      result.Select(row => row.Max).Should().Equal(1, 2, 4);
      result.Select(row => row.Min).Should().Equal(1, 2, 4);
   }

   [Fact]
   public async Task Should_execute_ntile_with_five_partition_columns()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.NTile(2,
                                                                     entity.Id % 2,
                                                                     entity.Id % 3,
                                                                     entity.Name,
                                                                     entity.Id > 1,
                                                                     entity.Id + 1,
                                                                     EF.Functions.OrderBy(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(1, 1, 1);
   }

   [Fact]
   public async Task Should_execute_ntile_with_two_to_four_partition_columns()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => new
                                                 {
                                                    Two = EF.Functions.NTile(2, entity.Id % 2, entity.Id % 3),
                                                    Three = EF.Functions.NTile(2, entity.Id % 2, entity.Id % 3, entity.Name),
                                                    Four = EF.Functions.NTile(2, entity.Id % 2, entity.Id % 3, entity.Name, entity.Id > 0)
                                                 })
                                .ToListAsync();

      // Every row is alone in its partition; ignoring the partitions would yield the buckets 1, 1, 2.
      result.Select(row => row.Two).Should().Equal(1, 1, 1);
      result.Select(row => row.Three).Should().Equal(1, 1, 1);
      result.Select(row => row.Four).Should().Equal(1, 1, 1);
   }

   [Fact]
   public async Task Should_execute_max_and_min_over_a_value_converted_column()
   {
      await using var context = await CreateContextAsync();
      context.TypedEntities.AddRange(new MySqlTypedEntity { Id = Guid.NewGuid(), Status = MySqlTestStatus.Draft, Flag = true },
                                     new MySqlTypedEntity { Id = Guid.NewGuid(), Status = MySqlTestStatus.Published, Flag = true });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();

      var result = await context.TypedEntities
                                .Select(entity => new
                                                 {
                                                    Max = EF.Functions.Max(entity.Status, entity.Flag),
                                                    Min = EF.Functions.Min(entity.Status, entity.Flag)
                                                 })
                                .ToListAsync();

      result.Should().OnlyContain(row => row.Max == MySqlTestStatus.Published && row.Min == MySqlTestStatus.Draft);
   }

   [Fact]
   public async Task Should_execute_a_window_function_with_one_argument_and_ordering()
   {
      await using var context = await SeedAsync();

      var result = await context.Entities.OrderBy(entity => entity.Id)
                                .Select(entity => EF.Functions.WindowFunction(_lead, entity.Id, EF.Functions.OrderBy(entity.Id)))
                                .ToListAsync();

      result.Should().Equal(2, 4, null);
   }

   private async Task<MySqlTestDbContext> SeedAsync()
   {
      var context = await CreateContextAsync();
      context.Entities.AddRange(new MySqlTestEntity { Id = 1, Name = "a" },
                                new MySqlTestEntity { Id = 2, Name = "b" },
                                new MySqlTestEntity { Id = 4, Name = "c" });
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();
      return context;
   }
}
