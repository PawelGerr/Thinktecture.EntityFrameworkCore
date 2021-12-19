using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.ObjectPool;

namespace Thinktecture.EntityFrameworkCore.Parameters;

/// <inheritdoc />
public class SqlServerCollectionParameterFactory : ICollectionParameterFactory
{
   private readonly ConcurrentDictionary<IEntityType, CollectionParameterInfo> _cache;
   private readonly JsonSerializerOptions _jsonSerializerOptions;
   private readonly ObjectPool<StringBuilder> _stringBuilderPool;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;

   /// <summary>
   /// Initializes new instance of <see cref="SqlServerCollectionParameterFactory"/>.
   /// </summary>
   /// <param name="jsonSerializerOptions">JSON serialization options.</param>
   /// <param name="stringBuilderPool">String builder pool.</param>
   /// <param name="sqlGenerationHelper"></param>
   /// <exception cref="ArgumentNullException">If <paramref name="jsonSerializerOptions"/> is <c>null</c>.</exception>
   public SqlServerCollectionParameterFactory(
      JsonSerializerOptions jsonSerializerOptions,
      ObjectPool<StringBuilder> stringBuilderPool,
      ISqlGenerationHelper sqlGenerationHelper)
   {
      _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
      _stringBuilderPool = stringBuilderPool ?? throw new ArgumentNullException(nameof(stringBuilderPool));
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
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

   /// <inheritdoc />
   public IQueryable<T> CreateComplexQuery<T>(DbContext ctx, IEnumerable<T> objects)
      where T : class
   {
      var entityType = ctx.Model.GetEntityType(typeof(T));
      var parameterInfo = _cache.GetOrAdd(entityType, GetComplexParameterInfo<T>);

      var parameter = new SqlParameter
                      {
                         DbType = DbType.String,
                         SqlDbType = SqlDbType.NVarChar,
                         Value = parameterInfo.ParameterFactory(objects, _jsonSerializerOptions)
                      };

      return ctx.Set<T>().FromSqlRaw(parameterInfo.Statement, parameter);
   }

   private CollectionParameterInfo GetScalarParameterInfo<T>(IEntityType entityType)
   {
      var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");

      var property = entityType.GetProperties().Single();

      var columnName = property.GetColumnName(storeObject) ?? throw new Exception($"The property '{property.Name}' has no column name.");
      var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(columnName);
      var columnType = property.GetColumnType(storeObject);
      var converter = property.GetValueConverter();
      var sb = _stringBuilderPool.Get();

      try
      {
         sb.Append("SELECT ").Append(escapedColumnName).Append(" FROM OPENJSON({0}, '$') WITH (")
           .Append(escapedColumnName).Append(" ").Append(columnType).Append(" '$')");

         var parameterFactory = CreateParameterFactory<T>(converter);

         return new CollectionParameterInfo(sb.ToString(), parameterFactory);
      }
      finally
      {
         _stringBuilderPool.Return(sb);
      }
   }

   private CollectionParameterInfo GetComplexParameterInfo<T>(IEntityType entityType)
   {
      var sqlStatement = CreateSqlStatementForComplexType(entityType);
      var parameterFactory = CreateParameterFactory<T>(null);

      return new CollectionParameterInfo(sqlStatement, parameterFactory);
   }

   private string CreateSqlStatementForComplexType(IEntityType entityType)
   {
      var sb = _stringBuilderPool.Get();
      var withClause = _stringBuilderPool.Get();

      try
      {
         var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");

         sb.Append("SELECT ");

         var isFirst = true;

         foreach (var property in entityType.GetProperties())
         {
            if (!isFirst)
            {
               sb.Append(", ");
               withClause.Append(", ");
            }

            var columnName = property.GetColumnName(storeObject) ?? throw new Exception($"The property '{property.Name}' has no column name.");
            var escapedColumnName = _sqlGenerationHelper.DelimitIdentifier(columnName);
            var columnType = property.GetColumnType(storeObject) ?? throw new Exception($"The property '{property.Name}' has no column type.");

            sb.Append("[").Append(columnName).Append("]");

            withClause.Append(escapedColumnName).Append(" ").Append(columnType).Append($" '$.{property.Name}'");

            isFirst = false;
         }

         sb.Append(" FROM OPENJSON({0}, '$') WITH (").Append(withClause).Append(")");

         return sb.ToString();
      }
      finally
      {
         _stringBuilderPool.Return(sb);
         _stringBuilderPool.Return(withClause);
      }
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
