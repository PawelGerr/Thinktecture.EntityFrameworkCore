using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.Data;

/// <summary>
/// Builds and caches property getters.
/// </summary>
public class PropertyGetterCache : IPropertyGetterCache
{
   private readonly ILogger<PropertyGetterCache> _logger;
   private readonly ConcurrentDictionary<PropertyWithNavigations, Delegate> _propertyGetterLookup;

   /// <summary>
   /// Initializes new instance of <see cref="PropertyGetterCache"/>.
   /// </summary>
   /// <param name="loggerFactory">Logger factory.</param>
   public PropertyGetterCache(ILoggerFactory loggerFactory)
   {
      _logger = loggerFactory?.CreateLogger<PropertyGetterCache>() ?? throw new ArgumentNullException(nameof(loggerFactory));
      _propertyGetterLookup = new ConcurrentDictionary<PropertyWithNavigations, Delegate>();
   }

   /// <inheritdoc />
   public Func<DbContext, TEntity, object?> GetPropertyGetter<TEntity>(PropertyWithNavigations property)
   {
      return (Func<DbContext, TEntity, object?>)_propertyGetterLookup.GetOrAdd(property, BuildPropertyGetter<TEntity>);
   }

   private Func<DbContext, TEntity, object?> BuildPropertyGetter<TEntity>(PropertyWithNavigations cacheKey)
   {
      var property = cacheKey.Property;
      var storeObject = cacheKey.GetStoreObject();
      var hasSqlDefaultValue = property.GetDefaultValueSql(storeObject) != null;
      var hasDefaultValue = property.TryGetDefaultValue(storeObject, out _);

      if ((hasSqlDefaultValue || hasDefaultValue) && !property.IsNullable)
      {
         if (property.ClrType.IsClass)
         {
            _logger.LogWarning("The corresponding column of '{Entity}.{Property}' has a DEFAULT value constraint in the database and is NOT NULL. Depending on the database vendor the .NET value `null` may lead to an exception because the tool for bulk insert of data may prevent sending `null`s for NOT NULL columns. Use 'PropertiesToInsert/PropertiesToUpdate' on corresponding options to specify properties to insert/update and skip the property so database uses the DEFAULT value.",
                               property.DeclaringType.ClrType.Name, property.Name);
         }
         else if (!property.ClrType.IsGenericType ||
                  (!property.ClrType.IsGenericTypeDefinition && property.ClrType.GetGenericTypeDefinition() != typeof(Nullable<>)))
         {
            _logger.LogWarning("The corresponding column of '{Entity}.{Property}' has a DEFAULT value constraint in the database and is NOT NULL. Depending on the database vendor the \".NET default values\" (`false`, `0`, `00000000-0000-0000-0000-000000000000` etc.) may lead to unexpected results because these values are sent to the database as-is, i.e. the DEFAULT value constraint will NOT be used by database. Use 'PropertiesToInsert/PropertiesToUpdate' on corresponding options to specify properties to insert and skip the property so database uses the DEFAULT value.",
                               property.DeclaringType.ClrType.Name, property.Name);
         }
      }

      var getter = BuildGetter(property);
      var converter = property.GetValueConverter();

      if (converter != null)
         getter = UseConverter(getter, converter);

      if (cacheKey.Navigations.Count != 0)
      {
         var naviGetter = BuildNavigationGetter(cacheKey.Navigations);
         getter = Combine(naviGetter, getter);
      }

      return (Func<DbContext, TEntity, object?>)(object)getter;
   }

   private static Func<object, object?> BuildNavigationGetter(IReadOnlyList<INavigation> navigations)
   {
      Func<object, object?>? getter = null;

      for (var i = 0; i < navigations.Count; i++)
      {
         getter = Combine(getter, navigations[i].GetGetter().GetClrValue);
      }

      return getter ?? throw new ArgumentException("No navigations provided.");
   }

   private static Func<object, object?> Combine(Func<object, object?>? parentGetter, Func<object, object?> getter)
   {
      if (parentGetter is null)
         return getter;

      return e =>
             {
                var childEntity = parentGetter(e);

                return childEntity == null ? null : getter(childEntity);
             };
   }

   private static Func<DbContext, object, object?> Combine(Func<object, object?> naviGetter, Func<DbContext, object, object?> getter)
   {
      return (ctx, e) =>
             {
                var childEntity = naviGetter(e);

                return childEntity == null ? null : getter(ctx, childEntity);
             };
   }

   private static Func<DbContext, object, object?> BuildGetter(IProperty property)
   {
      if (property.IsShadowProperty())
      {
         var shadowPropGetter = CreateShadowPropertyGetter(property);
         return shadowPropGetter.GetValue;
      }

      var getter = property.GetGetter();

      if (getter == null)
         throw new ArgumentException($"The property '{property.Name}' of entity '{property.DeclaringType.Name}' has no property getter.");

      return (_, entity) => getter.GetClrValueUsingContainingEntity(entity);
   }

   private static Func<DbContext, object, object?> UseConverter(Func<DbContext, object, object?> getter, ValueConverter converter)
   {
      var convert = converter.ConvertToProvider;

      return (ctx, e) =>
             {
                var value = getter(ctx, e);

                if (value != null)
                   value = convert(value);

                return value;
             };
   }

   [SuppressMessage("Usage", "EF1001", MessageId = "Internal EF Core API usage.")]
   private static IShadowPropertyGetter CreateShadowPropertyGetter(IProperty property)
   {
      var currentValueGetter = property.GetPropertyAccessors().CurrentValueGetter;
      var shadowPropGetterType = typeof(ShadowPropertyGetter<>).MakeGenericType(property.ClrType);
      var shadowPropGetter = Activator.CreateInstance(shadowPropGetterType, currentValueGetter)
                             ?? throw new Exception($"Could not create shadow property getter of type '{shadowPropGetterType.ShortDisplayName()}'.");

      return (IShadowPropertyGetter)shadowPropGetter;
   }
}
