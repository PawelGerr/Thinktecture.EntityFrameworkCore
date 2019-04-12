using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Thinktecture.EntityFrameworkCore.Infrastructure;

namespace Thinktecture
{
   public class TestBase
   {
      private readonly DbContextOptions<DbContextWithSchema> _option;
      private DbContextWithSchema _ctx;

      // use different schemas because EF Core uses static cache
      [NotNull]
      protected DbContextWithSchema DbContextWithRandomSchema => _ctx ?? (_ctx = new DbContextWithSchema(_option, Guid.NewGuid().ToString()));

      protected TestBase()
      {
         _option = new DbContextOptionsBuilder<DbContextWithSchema>()
                   .UseInMemoryDatabase("TestDatabase")
                   .ReplaceService<IModelCacheKeyFactory, DbSchemaAwareModelCacheKeyFactory>()
                   .Options;
      }

      [NotNull]
      protected static DbContextWithSchema CreateContextWithSchema(string schema)
      {
         var options = new DbContextOptionsBuilder<DbContextWithSchema>().Options;
         return new DbContextWithSchema(options, schema);
      }

      [NotNull]
      protected DbContextWithoutSchema CreateContextWithoutSchema()
      {
         var options = new DbContextOptionsBuilder<DbContextWithoutSchema>().Options;
         return new DbContextWithoutSchema(options);
      }
   }
}
