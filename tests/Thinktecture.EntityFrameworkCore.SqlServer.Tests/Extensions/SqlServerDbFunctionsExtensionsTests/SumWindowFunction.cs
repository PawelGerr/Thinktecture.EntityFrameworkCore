using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.SqlServerDbFunctionsExtensionsTests;

public class SumWindowFunction : IntegrationTestsBase
{
   public SumWindowFunction(ITestOutputHelper testOutputHelper, SqlServerFixture sqlServerFixture)
      : base(testOutputHelper, sqlServerFixture)
   {
   }

   [Fact]
   public void Returns_empty_result_set_if_table_is_empty()
   {
      ActDbContext.TestEntities
                  .Select(e => EF.Functions.Sum(e.Count, e.Name))
                  .Should().BeEmpty();

      ActDbContext.TestEntities
                  .Select(e => EF.Functions.Sum(e.Count, e.Name, EF.Functions.OrderBy(e.Id)))
                  .Should().BeEmpty();
   }

   [Fact]
   public void Generates_Sum_with_partition_by_for_not_nulls()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               Sum = EF.Functions.Sum(e.Count * 2, e.Name)
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(10));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(8));
   }

   [Fact]
   public void Generates_Sum_with_partition_over_2_columns_by_for_not_nulls()
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
                                               Sum = EF.Functions.Sum(e.Count, e.Name, e.RequiredName)
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public void Generates_Sum_with_partition_by_for_nulls()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", NullableCount = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               Sum = EF.Functions.Sum(e.NullableCount, e.Name)
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(4));
   }

   [Fact]
   public void Generates_Sum_with_partition_by_for_mixed_nullability()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", RequiredName = "1", NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", RequiredName = "2", NullableCount = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", RequiredName = "3", NullableCount = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.RequiredName,
                                               Sum = EF.Functions.Sum(e.NullableCount, e.Name)
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(3));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(4));
   }

   [Fact]
   public void Generates_Sum_with_partition_by_and_order_by_for_not_nulls()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 2 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Sum = EF.Functions.Sum(e.Count, e.Name, EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(2);
      result.Single(t => t.Name == "1" && t.Count == 3).Sum.Should().Be(5);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public void Generates_Sum_with_partition_over_2_columns_by_and_order_by_for_not_nulls()
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
                                               Sum = EF.Functions.Sum(e.Count, e.Name, e.RequiredName, EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.RequiredName == "1").Sum.Should().Be(2);
      result.Single(t => t.Name == "1" && t.RequiredName == "2").Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public void Generates_Sum_with_partition_by_and_order_by_for_nulls()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Sum = EF.Functions.Sum(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Where(t => t.Name == "1").Should().AllSatisfy(t => t.Sum.Should().Be(null));
      result.Where(t => t.Name == "2").Should().AllSatisfy(t => t.Sum.Should().Be(4));
   }

   [Fact]
   public void Generates_Sum_with_partition_by_and_order_by_for_mixed_nullability()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Sum = EF.Functions.Sum(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Sum.Should().Be(null);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }

   [Fact]
   public void Generates_Sum_with_partition_by_and_order_by_for_mixed_nullability_different_order()
   {
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("4883F7E0-FC8C-45FF-A579-DF351A3E79BA"), Name = "1", Count = 1, NullableCount = 3 });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("A9A6E809-1002-493A-BA5D-1483F704CBA9"), Name = "1", Count = 2, NullableCount = null });
      ArrangeDbContext.TestEntities.Add(new TestEntity { Id = new Guid("18C13F68-0981-4853-92FC-FB7B2551F70B"), Name = "2", Count = 3, NullableCount = 4 });
      ArrangeDbContext.SaveChanges();

      var result = ActDbContext.TestEntities
                               .Select(e => new
                                            {
                                               e.Name,
                                               e.Count,
                                               Sum = EF.Functions.Sum(e.NullableCount, e.Name, EF.Functions.OrderBy(e.Count))
                                            })
                               .ToList();

      result.Should().HaveCount(3);
      result.Single(t => t.Name == "1" && t.Count == 1).Sum.Should().Be(3);
      result.Single(t => t.Name == "1" && t.Count == 2).Sum.Should().Be(3);
      result.Single(t => t.Name == "2").Sum.Should().Be(4);
   }
}
