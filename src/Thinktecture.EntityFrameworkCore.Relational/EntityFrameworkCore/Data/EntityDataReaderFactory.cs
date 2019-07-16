using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Factory for <see cref="IEntityDataReader"/>.
   /// </summary>
   // ReSharper disable once ClassNeverInstantiated.Global
   public class EntityDataReaderFactory : IEntityDataReaderFactory
   {
      private readonly IPropertiesAccessorGenerator _generator;
      private readonly IMemoryCache _cache;

      /// <summary>
      /// Creates instances of <see cref="IEntityDataReader"/>.
      /// </summary>
      /// <param name="generator">Generates code required by <see cref="IEntityDataReader"/>.</param>
      /// <param name="cache">In-memory cache for caching the output of the code generation.</param>
      public EntityDataReaderFactory([NotNull] IPropertiesAccessorGenerator generator, IMemoryCache cache)
      {
         _generator = generator ?? throw new ArgumentNullException(nameof(generator));
         _cache = cache;
      }

      /// <inheritdoc />
      public IEntityDataReader Create<T>(IEnumerable<T> entities)
         where T : class
      {
         return Create(entities, GetRelevantProperties(typeof(T)));
      }

      /// <inheritdoc />
      public IEntityDataReader Create<T>(IEnumerable<T> entities, IReadOnlyList<PropertyInfo> properties)
         where T : class
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         var cachedItem = _cache.GetOrCreate(new EntityDataReaderFactoryCacheKey(typeof(T)), item => GenerateDataReaderDependencies<T>());

         return new EntityDataReader<T>(entities, properties, cachedItem.Properties, (Func<T, int, object>)cachedItem.GetValueDelegate);
      }

      [NotNull]
      private ReaderCacheItem GenerateDataReaderDependencies<T>()
      {
         var properties = GetRelevantProperties(typeof(T));
         var getValue = _generator.CreatePropertiesAccessor<T>(properties);

         return new ReaderCacheItem(properties, getValue);
      }

      [NotNull]
      private static IReadOnlyList<PropertyInfo> GetRelevantProperties([NotNull] Type type)
      {
         return type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
      }

      // use wrapper around the type so the cached items do not collide with items of other components
      private class EntityDataReaderFactoryCacheKey : IEquatable<EntityDataReaderFactoryCacheKey>
      {
         private readonly Type _type;

         public EntityDataReaderFactoryCacheKey([NotNull] Type type)
         {
            _type = type ?? throw new ArgumentNullException(nameof(type));
         }

         /// <inheritdoc />
         public bool Equals(EntityDataReaderFactoryCacheKey other)
         {
            return _type == other?._type;
         }

         /// <inheritdoc />
         public override bool Equals(object obj)
         {
            return Equals(obj as EntityDataReaderFactoryCacheKey);
         }

         /// <inheritdoc />
         public override int GetHashCode()
         {
            return _type.GetHashCode();
         }
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
