using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;

namespace Thinktecture.EntityFrameworkCore.Infrastructure.DbSchemaAwareModelCacheKeyFactoryTests
{
   public class TestModelCacheKeyFactory : IModelCacheKeyFactory
   {
      public Mock<IModelCacheKeyFactory> Mock { get; }

      public TestModelCacheKeyFactory()
      {
         Mock = new Mock<IModelCacheKeyFactory>();
         Mock.Setup(f => f.Create(It.IsAny<DbContext>())).Returns<DbContext>(ctx => new ModelCacheKey(ctx));
      }

      public object Create(DbContext context)
      {
         return Mock.Object.Create(context);
      }
   }
}