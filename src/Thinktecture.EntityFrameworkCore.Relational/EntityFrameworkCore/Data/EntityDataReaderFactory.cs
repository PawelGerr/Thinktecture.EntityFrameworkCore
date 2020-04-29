using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for <see cref="IEntityDataReader"/>.
   /// </summary>
   // ReSharper disable once ClassNeverInstantiated.Global
   public sealed class EntityDataReaderFactory : IEntityDataReaderFactory
   {
      private static readonly string _loggerName = $"{typeof(EntityDataReader<>).Namespace}.EntityDataReader";

      /// <inheritdoc />
      public IEntityDataReader Create<T>(
         DbContext ctx,
         IEnumerable<T> entities,
         IReadOnlyList<IProperty> properties)
         where T : class
      {
         if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         var logger = ctx.GetService<ILoggerFactory>().CreateLogger(_loggerName);

         return new EntityDataReader<T>(logger, ctx, entities, properties);
      }
   }
}
