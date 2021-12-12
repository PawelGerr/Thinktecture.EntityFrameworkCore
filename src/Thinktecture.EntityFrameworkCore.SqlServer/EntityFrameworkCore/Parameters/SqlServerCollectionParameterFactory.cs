using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Thinktecture.EntityFrameworkCore.Parameters;

/// <inheritdoc />
public class SqlServerCollectionParameterFactory : ICollectionParameterFactory
{
   private readonly ConcurrentDictionary<IEntityType, CollectionParameterInfo> _cache;
   private readonly JsonSerializerOptions _jsonSerializerOptions;

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerCollectionParameterFactory"/>.
   /// </summary>
   /// <param name="jsonSerializerOptions">JSON serialization options.</param>
   /// <exception cref="ArgumentNullException">If <paramref name="jsonSerializerOptions"/> is <c>null</c>.</exception>
   public SqlServerCollectionParameterFactory(JsonSerializerOptions jsonSerializerOptions)
   {
      _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
      _cache = new ConcurrentDictionary<IEntityType, CollectionParameterInfo>();
   }

   /// <inheritdoc />
   public IQueryable<T> CreateScalarQuery<T>(DbContext ctx, IEnumerable<T> values)
   {
      var entityType = ctx.Model.GetEntityType(typeof(ScalarCollectionParameter<T>));
      var parameterInfo = _cache.GetOrAdd(entityType, GetScalarParameterInfo<T>);

      var parameter = new SqlParameter
                      {
                         DbType = DbType.String,
                         SqlDbType = SqlDbType.NVarChar,
                         Value = parameterInfo.ParameterFactory(values, _jsonSerializerOptions)
                      };

      return ctx.Set<ScalarCollectionParameter<T>>().FromSqlRaw(parameterInfo.Statement, parameter).Select(e => e.Value);
   }

   private static CollectionParameterInfo GetScalarParameterInfo<T>(IEntityType entityType)
   {
      var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");

      var property = entityType.GetProperties().Single();

      var columnName = property.GetColumnName(storeObject);
      var columnType = property.GetColumnType(storeObject);
      var converter = property.GetValueConverter();

      var sqlStatement = $@"SELECT [{columnName}] FROM OPENJSON({{0}}, '$') WITH ([{columnName}] {columnType} '$')";

      var parameterFactory = CreateParameterFactory<T>(converter);

      return new CollectionParameterInfo(sqlStatement, parameterFactory);
   }

   private static Func<IEnumerable, JsonSerializerOptions, JsonCollectionParameter> CreateParameterFactory<T>(
      ValueConverter? converter)
   {
      if (converter is null)
         return (values, options) => new JsonCollectionParameter<T>((IEnumerable<T>)values, options);

      var itemType = typeof(T);
      var parameterType = typeof(JsonCollectionParameter<,>).MakeGenericType(itemType, converter.ProviderClrType);

      var valuesParam = Expression.Parameter(typeof(IEnumerable));
      var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions));

      var ctor = parameterType.GetConstructors().Single();
      var ctorCall = Expression.New(ctor,
                                    Expression.Convert(valuesParam, typeof(IEnumerable<T>)),
                                    optionsParam,
                                    Expression.Constant(converter.ConvertToProvider));

      return Expression.Lambda<Func<IEnumerable, JsonSerializerOptions, JsonCollectionParameter>>(ctorCall, valuesParam, optionsParam)
                       .Compile();
   }

   private readonly record struct CollectionParameterInfo(string Statement, Func<IEnumerable, JsonSerializerOptions, JsonCollectionParameter> ParameterFactory);
}
