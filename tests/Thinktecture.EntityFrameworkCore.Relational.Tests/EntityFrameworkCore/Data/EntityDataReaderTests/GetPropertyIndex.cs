using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderTests;

public class GetPropertyIndex : IntegrationTestsBase
{
   private readonly List<PropertyWithNavigations> _propertiesToRead = new();
   private readonly PropertyWithNavigations _column1;
   private readonly PropertyWithNavigations _column2;

   private EntityDataReader<TestEntity>? _sut;

   // ReSharper disable once InconsistentNaming
   private EntityDataReader<TestEntity> SUT => _sut ??= new EntityDataReader<TestEntity>(ActDbContext, new PropertyGetterCache(LoggerFactory), Array.Empty<TestEntity>(), _propertiesToRead, false);

   public GetPropertyIndex(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
      _column1 = new PropertyWithNavigations(ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column1)), Array.Empty<INavigation>());
      _column2 = new PropertyWithNavigations(ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column2)), Array.Empty<INavigation>());
   }

   [Fact]
   public void Should_throw_if_property_is_in_propertiesToRead()
   {
      _propertiesToRead.Add(_column1);

      SUT.Invoking(sut => sut.GetPropertyIndex(_column2))
         .Should().Throw<ArgumentException>();
   }

   [Fact]
   public void Should_return_index_of_the_property()
   {
      _propertiesToRead.Add(_column1);
      _propertiesToRead.Add(_column2);

      SUT.GetPropertyIndex(_column2).Should().Be(1);
   }

   protected override void Dispose(bool disposing)
   {
      _sut?.Dispose();

      base.Dispose(disposing);
   }
}
