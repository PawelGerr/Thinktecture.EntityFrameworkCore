using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
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
      _jsonSerializerOptions = new JsonSerializerOptions(jsonSerializerOptions);
      _jsonSerializerOptions.Converters.Add(new DecimalStringConverter());
      _jsonSerializerOptions.Converters.Add(new TimeSpanStringConverter());
      _jsonSerializerOptions.Converters.Add(new DateTimeStringConverter());
      _sqlGenerationHelper = sqlGenerationHelper ?? throw new ArgumentNullException(nameof(sqlGenerationHelper));
      _typeMappingSource = typeMappingSource ?? throw new ArgumentNullException(nameof(typeMappingSource));
   }

   /// <inheritdoc />
   public IQueryable<T> CreateScalarQuery<T>(DbContext ctx, IReadOnlyCollection<T> values, bool applyDistinct)
   {
      var entityName = EntityNameProvider.GetCollectionParameterName(typeof(T), true);
      var entityType = ctx.Model.GetEntityType(entityName);
      var info = _cache.GetOrAdd(entityType, type => CreateScalarInfo(type));
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
      var info = _cache.GetOrAdd(entityType, CreateComplexInfo);
      var json = JsonSerializer.Serialize(objects, _jsonSerializerOptions);

      return ctx.Set<T>(entityName)
                .FromSqlRaw(applyDistinct ? info.StatementWithDistinct : info.Statement, objects.Count, json);
   }

   private CollectionParameterInfo CreateScalarInfo(IEntityType entityType)
   {
      var property = entityType.GetProperties().Single();
      var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
                        ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{entityType.Name}'.");
      var columnName = property.GetColumnName(storeObject)
                       ?? throw new InvalidOperationException($"The property '{property.Name}' has no column name.");
      var columnType = GetColumnType(property, storeObject);
      var column = _sqlGenerationHelper.DelimitIdentifier(columnName);
      var columns = $"{column} {columnType} PATH '$'";

      return new CollectionParameterInfo(
         $"SELECT {column} FROM JSON_TABLE({{1}}, '$[*]' COLUMNS ({columns})) AS `collection` LIMIT {{0}}",
         $"SELECT DISTINCT {column} FROM JSON_TABLE({{1}}, '$[*]' COLUMNS ({columns})) AS `collection` LIMIT {{0}}");
   }

   private CollectionParameterInfo CreateComplexInfo(IEntityType entityType)
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
         var columnType = GetColumnType(property, storeObject);

         columns.Append(jsonColumn).Append(' ').Append(columnType).Append(" PATH ").Append(jsonPath);
         selections.Append("`collection`.").Append(jsonColumn).Append(" AS ").Append(_sqlGenerationHelper.DelimitIdentifier(columnName));
      }

      var from = $" FROM JSON_TABLE({{1}}, '$[*]' COLUMNS ({columns})) AS `collection` LIMIT {{0}}";
      return new CollectionParameterInfo($"SELECT {selections}{from}", $"SELECT DISTINCT {selections}{from}");
   }

   private string GetColumnType(IProperty property, StoreObjectIdentifier storeObject)
   {
      return property.GetColumnType(storeObject);
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

      // MySQL TIME accepts hours beyond 23 (up to 838), unlike the .NET constant format "d.hh:mm:ss".
      public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
      {
         var ticks = Math.Abs(value.Ticks);
         var text = String.Create(CultureInfo.InvariantCulture,
                                  $"{(value.Ticks < 0 ? "-" : "")}{ticks / TimeSpan.TicksPerHour:00}:{ticks / TimeSpan.TicksPerMinute % 60:00}:{ticks / TimeSpan.TicksPerSecond % 60:00}.{ticks % TimeSpan.TicksPerSecond / 10:000000}");
         writer.WriteStringValue(text);
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
}
