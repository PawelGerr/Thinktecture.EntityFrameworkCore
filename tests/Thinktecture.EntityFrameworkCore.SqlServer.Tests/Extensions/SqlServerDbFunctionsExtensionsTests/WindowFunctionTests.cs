using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.SqlServerDbFunctionsExtensionsTests;

public class WindowFunctionTests : IntegrationTestsBase
{
   private static readonly WindowFunction<int> _averageInt = new("AVG");
   private static readonly WindowFunction<long> _rank = new("RANK");
   private static readonly WindowFunction<int> _count = new("COUNT", true);

   public WindowFunctionTests(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public void Should_translate_function_with_1_argument_and_1_partition_by()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               Average = EF.Functions.Average(e.Count * 2, e.Name),
                                               OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count * 2, EF.Functions.PartitionBy(e.Name))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
   }

   [Fact]
   public void Should_translate_function_with_1_argument_and_2_partition_by()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Average = EF.Functions.Average(e.Count, e.Name, e.RequiredName),
                                               OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name, e.RequiredName))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
   }

   [Fact]
   public void Should_translate_function_with_1_argument_and_1_partition_by_and_1_order_by()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 4 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 5 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Average = EF.Functions.Average(e.Count, e.Name, EF.Functions.OrderBy(e.Count)),
                                               OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name), EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
   }

   [Fact]
   public void Should_translate_function_with_1_argument_and_2_partition_by_and_1_order_by()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.RequiredName,
                                               e.Count,
                                               Average = EF.Functions.Average(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count)),
                                               OtherAverage = EF.Functions.WindowFunction(_averageInt, e.Count, EF.Functions.PartitionBy(e.Name, e.RequiredName), EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Should().AllSatisfy(t => t.Average.Should().Be(t.OtherAverage));
   }

   [Fact]
   public void Should_translate_function_without_arguments_and_1_order_by()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.RequiredName,
                                               e.Count,
                                               Rank = EF.Functions.WindowFunction(_rank, EF.Functions.OrderBy(e.RequiredName))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Rank.Should().Be(1);
      result.Single(t => t.RequiredName == "2").Rank.Should().Be(2);
      result.Single(t => t.RequiredName == "3").Rank.Should().Be(3);
   }

   [Fact]
   public void Should_translate_function_without_arguments_and_1_partition_by_and_1_order_by()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.RequiredName,
                                               e.Count,
                                               Rank = EF.Functions.WindowFunction(_rank, EF.Functions.PartitionBy(e.Name), EF.Functions.OrderBy(e.RequiredName))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Rank.Should().Be(1);
      result.Single(t => t.RequiredName == "2").Rank.Should().Be(2);
      result.Single(t => t.RequiredName == "3").Rank.Should().Be(1);
   }

   [Fact]
   public void Should_translate_function_without_parameters()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.RequiredName,
                                               Count = EF.Functions.WindowFunction(_count)
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "2").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "3").Count.Should().Be(3);
   }

   [Fact]
   public void Should_translate_function_with_1_parameter()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.RequiredName,
                                               Count = EF.Functions.WindowFunction(_count, e.Name)
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.RequiredName == "1").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "2").Count.Should().Be(3);
      result.Single(t => t.RequiredName == "3").Count.Should().Be(3);
   }
}
