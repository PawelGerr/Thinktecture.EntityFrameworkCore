using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DefaultSchemaRespectingModelCacheKeyFactoryTests
{
   public class TestModelCacheKeyFactory : IModelCacheKeyFactory
   {
      public Mock<IModelCacheKeyFactory> Mock { get; }

      public TestModelCacheKeyFactory()
      {
         Mock = new Mock<IModelCacheKeyFactory>();
         Mock.Setup(f => f.Create(It.IsAny<DbContext>(), It.IsAny<bool>())).Returns<DbContext, bool>((ctx, designTime) => new ModelCacheKey(ctx, designTime));
      }

      public object Create(DbContext context)
      {
         return Create(context, false);
      }

      public object Create(DbContext context, bool designTime)
      {
         return Mock.Object.Create(context, designTime);
      }
   }
}
