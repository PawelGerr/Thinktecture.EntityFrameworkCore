using Microsoft.EntityFrameworkCore.Metadata;
using Thinktecture.TestDatabaseContext;

namespace Thinktecture.EntityFrameworkCore.Data.EntityDataReaderTests;

public class Properties : IntegrationTestsBase
{
   private readonly List<PropertyWithNavigations> _propertiesToRead = new();
   private readonly PropertyWithNavigations _column1;
   private readonly PropertyWithNavigations _column2;

   private EntityDataReader<TestEntity>? _sut;

   // ReSharper disable once InconsistentNaming
   private EntityDataReader<TestEntity> SUT => _sut ??= new EntityDataReader<TestEntity>(ActDbContext, new PropertyGetterCache(LoggerFactory!), Array.Empty<TestEntity>(), _propertiesToRead, false);

   public Properties(ITestOutputHelper testOutputHelper)
      : base(testOutputHelper)
   {
      _column1 = new PropertyWithNavigations(ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column1)), Array.Empty<INavigation>());
      _column2 = new PropertyWithNavigations(ArrangeDbContext.GetEntityType<TestEntity>().GetProperty(nameof(TestEntity.Column2)), Array.Empty<INavigation>());
   }

   [Fact]
   public void Should_return_propertiesToRead()
   {
      _propertiesToRead.Add(_column1);
      _propertiesToRead.Add(_column2);

      SUT.Properties.Should().HaveCount(2);
      SUT.Properties.Should().Contain(_column1);
      SUT.Properties.Should().Contain(_column2);
   }

   protected override void Dispose(bool disposing)
   {
      _sut?.Dispose();

      base.Dispose(disposing);
   }
}
