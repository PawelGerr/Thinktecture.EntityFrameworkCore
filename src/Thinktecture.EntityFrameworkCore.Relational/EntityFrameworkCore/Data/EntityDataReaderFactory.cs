using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for <see cref="IEntityDataReader"/>.
   /// </summary>
   // ReSharper disable once ClassNeverInstantiated.Global
   public class EntityDataReaderFactory : IEntityDataReaderFactory
   {
      private readonly IPropertiesAccessorGenerator _generator;
      private readonly ConcurrentDictionary<IEntityType, ReaderCacheItem> _cache;

      /// <summary>
      /// Creates instances of <see cref="IEntityDataReader"/>.
      /// </summary>
      /// <param name="generator">Generates code required by <see cref="IEntityDataReader"/>.</param>
      public EntityDataReaderFactory([NotNull] IPropertiesAccessorGenerator generator)
      {
         _generator = generator ?? throw new ArgumentNullException(nameof(generator));
         _cache = new ConcurrentDictionary<IEntityType, ReaderCacheItem>();
      }

      /// <inheritdoc />
      public IEntityDataReader Create<T>(IEnumerable<T> entities, IEntityType entityType)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

         var cachedItem = _cache.GetOrAdd(entityType, type => GenerateDataReaderDependencies<T>(entityType));

         return new EntityDataReader<T>(entities, cachedItem.Properties, (Func<T, int, object>)cachedItem.GetValueDelegate);
      }

      [NotNull]
      private ReaderCacheItem GenerateDataReaderDependencies<T>([NotNull] IEntityType entityType)
      {
         var properties = entityType.GetProperties().Select(p => p.PropertyInfo).ToList().AsReadOnly();
         var getValue = _generator.CreatePropertiesAccessor<T>(properties);

         return new ReaderCacheItem(properties, getValue);
      }

      private class ReaderCacheItem
      {
         public IReadOnlyList<PropertyInfo> Properties { get; }
         public Delegate GetValueDelegate { get; }

         public ReaderCacheItem([NotNull] IReadOnlyList<PropertyInfo> properties, [NotNull] Delegate getValueDelegate)
         {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            GetValueDelegate = getValueDelegate ?? throw new ArgumentNullException(nameof(getValueDelegate));
         }
      }
   }
}
