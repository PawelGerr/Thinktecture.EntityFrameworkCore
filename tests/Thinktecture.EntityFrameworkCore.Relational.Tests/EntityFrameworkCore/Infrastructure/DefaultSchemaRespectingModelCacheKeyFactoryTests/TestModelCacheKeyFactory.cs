using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DefaultSchemaRespectingModelCacheKeyFactoryTests;

public class TestModelCacheKeyFactory : IModelCacheKeyFactory
{
   public IModelCacheKeyFactory Mock { get; }

   public TestModelCacheKeyFactory()
   {
      Mock = Substitute.For<IModelCacheKeyFactory>();
      Mock.Create(Arg.Any<DbContext>(), Arg.Any<bool>()).Returns(x => new ModelCacheKey((DbContext)x[0], (bool)x[1]));
   }

   public object Create(DbContext context)
   {
      return Create(context, false);
   }

   public object Create(DbContext context, bool designTime)
   {
      return Mock.Create(context, designTime);
   }
}
