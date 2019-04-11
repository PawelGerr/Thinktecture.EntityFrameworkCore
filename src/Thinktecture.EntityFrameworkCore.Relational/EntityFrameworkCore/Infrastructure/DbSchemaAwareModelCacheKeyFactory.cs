using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Thinktecture.EntityFrameworkCore.Infrastructure
{
   /// <summary>
   /// Cache key factory that takes the schema into account.
   /// </summary>
   public class DbSchemaAwareModelCacheKeyFactory : IModelCacheKeyFactory
   {
      /// <inheritdoc />
      [NotNull]
      public object Create(DbContext context)
      {
         if (context == null)
            throw new ArgumentNullException(nameof(context));

         return new
                {
                   Type = context.GetType(),
                   // ReSharper disable once SuspiciousTypeConversion.Global
                   Schema = context is IDbContextSchema schema ? schema.Schema : null
                };
      }
   }
}
