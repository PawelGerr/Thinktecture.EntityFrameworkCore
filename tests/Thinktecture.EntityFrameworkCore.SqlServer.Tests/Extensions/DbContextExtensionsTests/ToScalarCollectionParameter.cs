using System.Reflection;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.Extensions.DbContextExtensionsTests;

public class ToScalarCollectionParameter : IntegrationTestsBase
{
   public ToScalarCollectionParameter(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper, true)
   {
   }

   public static readonly IEnumerable<object[]> _values = new[]
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

   private static readonly MethodInfo _genericDataTypeTest = typeof(ToScalarCollectionParameter).GetMethod(nameof(MakeGenericToCollectionParameterTest), BindingFlags.Instance | BindingFlags.NonPublic)
                                                             ?? throw new Exception($"Method '{nameof(MakeGenericToCollectionParameterTest)}' not found.");

   [Theory]
   [MemberData(nameof(_values))]
   public void Should_work_with_default_data_types(object value)
   {
      _genericDataTypeTest.MakeGenericMethod(value.GetType()).Invoke(this, new[] { value });
   }

   private void MakeGenericToCollectionParameterTest<T>(T value)
   {
      ActDbContext.ToScalarCollectionParameter(new[] { value })
                  .ToList()
                  .Should().BeEquivalentTo(new[] { value });
   }

   [Fact]
   public async Task Should_work_with_contains()
   {
      var testEntity = new TestEntity
                       {
                          Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                          Name = "Name",
                          Count = 42
                       };
      await ArrangeDbContext.AddAsync(testEntity);
      await ArrangeDbContext.SaveChangesAsync();

      var collectionParameter = ActDbContext.ToScalarCollectionParameter(new[] { testEntity.Id });
      var loadedEntities = await ActDbContext.TestEntities
                                             .Where(e => collectionParameter.Contains(e.Id))
                                             .ToListAsync();

      loadedEntities.Should().HaveCount(1);
      var loadedEntity = loadedEntities[0];
      loadedEntity.Should().BeEquivalentTo(new TestEntity
                                           {
                                              Id = new Guid("7F8B0E79-2C91-4682-9F61-6FC86B4E5244"),
                                              Name = "Name",
                                              Count = 42
                                           });
   }
}
