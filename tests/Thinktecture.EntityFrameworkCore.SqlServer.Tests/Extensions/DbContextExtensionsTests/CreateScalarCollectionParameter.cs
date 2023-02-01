using System.Reflection;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

public class CreateScalarCollectionParameter : IntegrationTestsBase
{
   public CreateScalarCollectionParameter(ITestOutputHelper testOutputHelper, SqlServerContainerFixture sqlServerContainerFixture)
      : base(testOutputHelper, sqlServerContainerFixture)
   {
   }

   public static readonly IEnumerable<object[]> Values = new[]
                                                         {
                                                            new object[] { 42 },
                                                            new object[] { 43L },
                                                            new object[] { new DateTime(2021, 1, 15, 12, 30, 40) },
                                                            new object[] { new Guid("CE5E3D80-13D6-43C3-BB41-6499E2AD6B63") },
                                                            new object[] { true },
                                                            new object[] { (byte)4 },
                                                            new object[] { 4.2d },
                                                            new object[] { new DateTimeOffset(new DateTime(2021, 1, 15, 12, 30, 40), TimeSpan.FromMinutes(60)) },
                                                            new object[] { (short)5 },
                                                            new object[] { 4.3f },
                                                            new object[] { 4.4m },
                                                            new object[] { TimeSpan.FromMinutes(60) },
                                                            new object[] { "test" },
                                                            new object[] { new ConvertibleClass(99) }
                                                         };

   private static readonly MethodInfo _genericDataTypeTest = typeof(CreateScalarCollectionParameter).GetMethod(nameof(MakeGenericCreateCollectionParameterTest), BindingFlags.Instance | BindingFlags.NonPublic)
                                                             ?? throw new Exception($"Method '{nameof(MakeGenericCreateCollectionParameterTest)}' not found.");

   [Theory]
   [MemberData(nameof(Values))]
   public void Should_work_with_default_data_types_with_distinct(object value)
   {
      _genericDataTypeTest.MakeGenericMethod(value.GetType()).Invoke(this, new[] { value, true });
   }

   [Theory]
   [MemberData(nameof(Values))]
   public void Should_work_with_default_data_types_without_distinct(object value)
   {
      _genericDataTypeTest.MakeGenericMethod(value.GetType()).Invoke(this, new[] { value, false });
   }

   private void MakeGenericCreateCollectionParameterTest<T>(T value, bool applyDistinct)
   {
      ActDbContext.CreateScalarCollectionParameter(new[] { value }, applyDistinct)
                  .ToList()
                  .Should().BeEquivalentTo(new[] { value });
   }

   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task Should_work_with_contains(bool applyDistinct)
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.CreateScalarCollectionParameter(new[] { testEntity.Id }, applyDistinct);
      var loadedEntities = await ActDbContext.TestEntities
                                             .Where(e => collectionParameter.Contains(e.Id))
                                             .ToListAsync();

      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                                              Name = "Name",
                                              RequiredName = "RequiredName",
                                              Count = 42
                                           });
   }

   [Fact]
   public async Task Should_work_GroupBy_and_aggregate()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                          Name = "Name",
                          RequiredName = "RequiredName",
                          Count = 42
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.CreateScalarCollectionParameter(new[] { testEntity.Id });
      var loadedEntities = await ActDbContext.TestEntities
                                             .Where(e => collectionParameter.Contains(e.Id))
                                             .GroupBy(e => e.Id)
                                             .Select(g => new { g.Key, Aggregate = g.Count() })
                                             .ToListAsync();

      loadedEntities.Should().BeEquivalentTo(new[]
                                             {
                                                new
                                                {
                                                   Key = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                                                   Aggregate = 1
                                                }
                                             });
   }
}
