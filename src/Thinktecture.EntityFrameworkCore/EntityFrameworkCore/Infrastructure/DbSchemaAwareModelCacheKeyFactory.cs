using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   public class DbSchemaAwareModelCacheKeyFactory : IModelCacheKeyFactory
   {
      /// <inheritdoc />
      [NotNull]
      public object Create(DbContext context)
      {
         return new
                {
                   Type = context.GetType(),
                   Schema = context is IDbContextSchema schema ? schema.Schema : null
                };
      }
   }
}
