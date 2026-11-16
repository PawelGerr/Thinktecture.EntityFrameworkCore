using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Thinktecture.EntityFrameworkCore.Internal;
using Thinktecture.Internal;

namespace Thinktecture.EntityFrameworkCore.Parameters;

/// <summary>
/// Creates collection parameter queries for MySQL using JSON_TABLE.
/// </summary>
public sealed class MySqlCollectionParameterFactory : ICollectionParameterFactory
{
   private readonly ConcurrentDictionary<IEntityType, CollectionParameterInfo> _cache = new();
   private readonly JsonSerializerOptions _jsonSerializerOptions;
   private readonly ISqlGenerationHelper _sqlGenerationHelper;
   private readonly IRelationalTypeMappingSource _typeMappingSource;

   /// <summary>
   /// Initializes a new instance of <see cref="MySqlCollectionParameterFactory" />.
   /// </summary>
   /// <param name="jsonSerializerOptions">Options used to serialize the collection to JSON.</param>
   /// <param name="sqlGenerationHelper">SQL generation helper.</param>
   /// <param name="typeMappingSource">Type mapping source.</param>
   public MySqlCollectionParameterFactory(
      JsonSerializerOptions jsonSerializerOptions,
      ISqlGenerationHelper sqlGenerationHelper,
      IRelationalTypeMappingSource typeMappingSource)
   {
      ArgumentNullException.ThrowIfNull(jsonSerializerOptions);

      // MySQL parses JSON numbers as DOUBLE and rejects the .NET formats of TimeSpan/DateTime; these values are sent as strings instead.
      // A DateTimeOffset is sent as UTC without an offset, because MySQL converts a value with an offset into the session time zone.
      _jsonSerializerOptions = new JsonSerializerOptions(jsonSerializerOptions);
      _jsonSerializerOptions.Converters.Add(new DecimalStringConverter());
      _jsonSerializerOptions.Converters.Add(new TimeSpanStringConverter());
      _jsonSerializerOptions.Converters.Add(new DateTimeStringConverter());
      _jsonSerializerOptions.Converters.Add(new DateTimeOffsetStringConverter());
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   public IQueryable<T> CreateScalarQuery<T>(DbContext ctx, IReadOnlyCollection<T> values, bool applyDistinct)
   {
      var entityName = EntityNameProvider.GetCollectionParameterName(typeof(T), true);
      var entityType = ctx.Model.GetEntityType(entityName);
      var info = _cache.GetOrAdd(entityType, type => CreateScalarInfo(ctx, type));
      var property = entityType.GetProperties().Single();
      // GetValueConverter() returns an explicitly configured converter only; HasConversion<TProvider>() is resolved by the type mapping.
      var json = Serialize(values, property.GetValueConverter() ?? property.GetRelationalTypeMapping().Converter);

      return ctx.Set<ScalarCollectionParameter<T>>(entityName)
                .FromSqlRaw(applyDistinct ? info.StatementWithDistinct : info.Statement, values.Count, json)
                .Select(parameter => parameter.Value);
   }

   /// <inheritdoc />
   public IQueryable<T> CreateComplexQuery<T>(DbContext ctx, IReadOnlyCollection<T> objects, bool applyDistinct)
      where T : class
   {
      var entityName = EntityNameProvider.GetCollectionParameterName(typeof(T), false);
      var entityType = ctx.Model.GetEntityType(entityName);
      var info = _cache.GetOrAdd(entityType, type => CreateComplexInfo(ctx, type));
      var json = JsonSerializer.Serialize(objects, _jsonSerializerOptions);

      return ctx.Set<T>(entityName)
                .FromSqlRaw(applyDistinct ? info.StatementWithDistinct : info.Statement, objects.Count, json);
   }

   private CollectionParameterInfo CreateScalarInfo(DbContext ctx, IEntityType entityType)
   {
      var property = entityType.GetProperties().Single();
      var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
                        ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");
      var columnName = property.GetColumnName(storeObject)
                       ?? throw new InvalidOperationException($"The property '{property.Name}' has no column name.");
      var columnType = GetColumnDefinition(ctx, property, storeObject, columnName);
      var column = _sqlGenerationHelper.DelimitIdentifier(columnName);
      var columns = $"{column} {columnType} PATH '$' ERROR ON ERROR";

      return new CollectionParameterInfo(
         $"SELECT {column} FROM JSON_TABLE({{1}}, '$[*]' COLUMNS ({columns})) AS `collection` LIMIT {{0}}",
         $"SELECT DISTINCT {column} FROM JSON_TABLE({{1}}, '$[*]' COLUMNS ({columns})) AS `collection` LIMIT {{0}}");
   }

   private CollectionParameterInfo CreateComplexInfo(DbContext ctx, IEntityType entityType)
   {
      var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
                        ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");
      var columns = new StringBuilder();
      var selections = new StringBuilder();

      foreach (var property in entityType.GetProperties())
      {
         if (columns.Length != 0)
         {
            columns.Append(", ");
            selections.Append(", ");
         }

         var columnName = property.GetColumnName(storeObject)
                          ?? throw new InvalidOperationException($"The property '{property.Name}' has no column name.");
         var jsonColumn = _sqlGenerationHelper.DelimitIdentifier(property.Name);
         var jsonPath = _typeMappingSource.FindMapping(typeof(string))
                        ?.GenerateSqlLiteral("$." + JsonSerializer.Serialize(GetJsonPropertyName(property), _jsonSerializerOptions))
                        ?? throw new InvalidOperationException("MySQL string type mapping is unavailable.");
         var columnType = GetColumnDefinition(ctx, property, storeObject, columnName);

         columns.Append(jsonColumn).Append(' ').Append(columnType).Append(" PATH ").Append(jsonPath).Append(" ERROR ON ERROR");
         selections.Append("`collection`.").Append(jsonColumn).Append(" AS ").Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
      }

      var from = $" FROM JSON_TABLE({{1}}, '$[*]' COLUMNS ({columns})) AS `collection` LIMIT {{0}}";
      return new CollectionParameterInfo($"SELECT {selections}{from}", $"SELECT DISTINCT {selections}{from}");
   }

   // ERROR ON ERROR: a value that does not fit the column type raises an error instead of becoming NULL.
   // The charset and the collation make a comparison with a collated column possible (error 1267 otherwise).
   private static string GetColumnDefinition(DbContext ctx, IProperty property, StoreObjectIdentifier storeObject, string columnName)
   {
      var columnType = property.GetColumnType(storeObject);

      return MySqlColumnCharSet.SupportsCharSet(columnType)
                ? columnType + MySqlColumnCharSet.GetColumnClauses(ctx.GetService<IDesignTimeModel>().Model, storeObject, columnName, columnType)
                : columnType;
   }

   private string GetJsonPropertyName(IProperty property)
   {
      return property.PropertyInfo?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
             ?? _jsonSerializerOptions.PropertyNamingPolicy?.ConvertName(property.Name)
             ?? property.Name;
   }

   private string Serialize<T>(IReadOnlyCollection<T> values, Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter? converter)
   {
      if (converter is null)
         return JsonSerializer.Serialize(values, _jsonSerializerOptions);

      var convertedValues = values.Select(value => converter.ConvertToProvider(value)).ToArray();
      return JsonSerializer.Serialize(convertedValues, _jsonSerializerOptions);
   }

   private sealed record CollectionParameterInfo(string Statement, string StatementWithDistinct);

   private sealed class DecimalStringConverter : JsonConverter<decimal>
   {
      public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
         return reader.TokenType == JsonTokenType.String
                   ? Decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
                   : reader.GetDecimal();
      }

      public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
      {
         writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
      }
   }

   private sealed class TimeSpanStringConverter : JsonConverter<TimeSpan>
   {
      public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
         var value = reader.GetString()!;
         var isNegative = value.StartsWith('-');
         var parts = value.TrimStart('-').Split(':');
         var secondsParts = parts[2].Split('.');
         var ticks = Int64.Parse(parts[0], CultureInfo.InvariantCulture) * TimeSpan.TicksPerHour
                     + Int64.Parse(parts[1], CultureInfo.InvariantCulture) * TimeSpan.TicksPerMinute
                     + Int64.Parse(secondsParts[0], CultureInfo.InvariantCulture) * TimeSpan.TicksPerSecond;

         if (secondsParts.Length > 1)
            ticks += Int64.Parse(secondsParts[1].PadRight(7, '0')[..7], CultureInfo.InvariantCulture);

         return TimeSpan.FromTicks(isNegative ? -ticks : ticks);
      }

      public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
      {
         writer.WriteStringValue(MySqlTimeFormatter.Format(value));
      }
   }

   private sealed class DateTimeStringConverter : JsonConverter<DateTime>
   {
      public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
         return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
      }

      // .NET writes 7 fractional digits and a 'T' separator; MySQL DATETIME(6) takes 6 digits.
      public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
      {
         writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture));
      }
   }

   private sealed class DateTimeOffsetStringConverter : JsonConverter<DateTimeOffset>
   {
      public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
         return DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
      }

      // The provider stores a DateTimeOffset as UTC. MySQL converts a value with an offset into the session time zone.
      public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
      {
         writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture));
      }
   }
}
