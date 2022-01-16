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
using Thinktecture.Internal;

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
   public IQueryable<T> CreateScalarQuery<T>(DbContext ctx, IReadOnlyCollection<T> values, bool applyDistinct)
   {
      var entityName = EntityNameProvider.GetCollectionParameterName(typeof(T), true);
      var entityType = ctx.Model.GetEntityType(entityName);
      var parameterInfo = _cache.GetOrAdd(entityType,
                                          static (type, args) => GetScalarParameterInfo<T>(args._stringBuilderPool, args._sqlGenerationHelper, type),
                                          (_stringBuilderPool, _sqlGenerationHelper));

      var parameterValue = parameterInfo.ParameterFactory(values, _jsonSerializerOptions);

      return ctx.Set<ScalarCollectionParameter<T>>(entityName)
                .FromSqlRaw(applyDistinct ? parameterInfo.StatementWithDistinct : parameterInfo.Statement,
                            CreateTopParameter(parameterValue),
                            CreateJsonParameter(parameterValue))
                .Select(e => e.Value);
   }

   /// <inheritdoc />
   public IQueryable<T> CreateComplexQuery<T>(DbContext ctx, IReadOnlyCollection<T> objects, bool applyDistinct)
      where T : class
   {
      var entityName = EntityNameProvider.GetCollectionParameterName(typeof(T), false);
      var entityType = ctx.Model.GetEntityType(entityName);
      var parameterInfo = _cache.GetOrAdd(entityType, GetComplexParameterInfo<T>);

      var parameterValue = parameterInfo.ParameterFactory(objects, _jsonSerializerOptions);

      return ctx.Set<T>(entityName)
                .FromSqlRaw(applyDistinct ? parameterInfo.StatementWithDistinct : parameterInfo.Statement,
                            CreateTopParameter(parameterValue),
                            CreateJsonParameter(parameterValue));
   }

   private static SqlParameter CreateJsonParameter(JsonCollectionParameter parameterValue)
   {
      return new SqlParameter
             {
                DbType = DbType.String,
                SqlDbType = SqlDbType.NVarChar,
                Value = parameterValue
             };
   }

   private static SqlParameter CreateTopParameter(JsonCollectionParameter jsonCollection)
   {
      return new SqlParameter
             {
                DbType = DbType.Int64,
                SqlDbType = SqlDbType.BigInt,
                Value = jsonCollection
             };
   }

   private static CollectionParameterInfo GetScalarParameterInfo<T>(
      ObjectPool<StringBuilder> stringBuilderPool,
      ISqlGenerationHelper sqlGenerationHelper,
      IEntityType entityType)
   {
      var property = entityType.GetProperties().Single();
      var converter = property.GetValueConverter();

      return new CollectionParameterInfo(BuildScalarStatement(stringBuilderPool, sqlGenerationHelper, entityType, property, false),
                                         BuildScalarStatement(stringBuilderPool, sqlGenerationHelper, entityType, property, true),
                                         CreateParameterFactory<T>(converter));
   }

   private static string BuildScalarStatement(
      ObjectPool<StringBuilder> stringBuilderPool,
      ISqlGenerationHelper sqlGenerationHelper,
      IEntityType entityType,
      IProperty property,
      bool applyDistinct)
   {
      var sb = stringBuilderPool.Get();

      try
      {
         var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");
         var columnType = property.GetColumnType(storeObject);
         var columnName = property.GetColumnName(storeObject) ?? throw new Exception($"The property '{property.Name}' has no column name.");
         var escapedColumnName = sqlGenerationHelper.DelimitIdentifier(columnName);

         sb.Append("SELECT ");

         if (applyDistinct)
            sb.Append("DISTINCT ");

         sb.Append("TOP({0}) ").Append(escapedColumnName).Append(" FROM OPENJSON({1}, '$') WITH (")
           .Append(escapedColumnName).Append(" ").Append(columnType).Append(" '$')");

         return sb.ToString();
      }
      finally
      {
         stringBuilderPool.Return(sb);
      }
   }

   private CollectionParameterInfo GetComplexParameterInfo<T>(IEntityType entityType)
   {
      return new CollectionParameterInfo(CreateSqlStatementForComplexType(entityType, false),
                                         CreateSqlStatementForComplexType(entityType, true),
                                         CreateParameterFactory<T>(null));
   }

   private string CreateSqlStatementForComplexType(IEntityType entityType, bool withDistinct)
   {
      var sb = _stringBuilderPool.Get();
      var withClause = _stringBuilderPool.Get();

      try
      {
         var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) ?? throw new Exception($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");

         sb.Append("SELECT ");

         if (withDistinct)
            sb.Append("DISTINCT ");

         sb.Append("TOP({0}) ");

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

            sb.Append(escapedColumnName);
            withClause.Append(escapedColumnName).Append(" ").Append(columnType).Append($" '$.{property.Name}'");

            isFirst = false;
         }

         sb.Append(" FROM OPENJSON({1}, '$') WITH (").Append(withClause).Append(")");

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
         return static (values, options) => new JsonCollectionParameter<T>((IReadOnlyCollection<T>)values, options);

      var itemType = typeof(T);
      var parameterType = typeof(JsonCollectionParameter<,>).MakeGenericType(itemType, converter.ProviderClrType);

      var valuesParam = Expression.Parameter(typeof(IEnumerable));
      var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions));

      var ctor = parameterType.GetConstructors().Single();
      var ctorCall = Expression.New(ctor,
                                    Expression.Convert(valuesParam, typeof(IReadOnlyCollection<T>)),
                                    optionsParam,
                                    Expression.Constant(converter.ConvertToProvider));

      return Expression.Lambda<Func<IEnumerable, JsonSerializerOptions, JsonCollectionParameter>>(ctorCall, valuesParam, optionsParam)
                       .Compile();
   }

   private readonly record struct CollectionParameterInfo(string Statement, string StatementWithDistinct, Func<IEnumerable, JsonSerializerOptions, JsonCollectionParameter> ParameterFactory);
}
