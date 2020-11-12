using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Data reader for Entity Framework Core entities.
   /// </summary>
   /// <typeparam name="T">Type of the entity.</typeparam>
   [SuppressMessage("ReSharper", "EF1001")]
   public sealed class EntityDataReader<T> : IEntityDataReader
      where T : class
   {
      private readonly ILogger _logger;
      private readonly DbContext _ctx;
      private readonly IEnumerator<T> _enumerator;
      private readonly Dictionary<int, Func<T, object?>> _propertyGetterLookup;

      /// <inheritdoc />
      public IReadOnlyList<IProperty> Properties { get; }

      /// <inheritdoc />
      public int FieldCount => Properties.Count;

      /// <summary>
      /// Initializes <see cref="EntityDataReader{T}"/>
      /// </summary>
      /// <param name="logger">Logger.</param>
      /// <param name="ctx">Database context.</param>
      /// <param name="entities">Entities to read.</param>
      /// <param name="properties">Properties to read.</param>
      public EntityDataReader(
         ILogger logger,
         DbContext ctx,
         IEnumerable<T> entities,
         IReadOnlyList<IProperty> properties)
      {
         if (entities == null)
            throw new ArgumentNullException(nameof(entities));
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));
         if (properties.Count == 0)
            throw new ArgumentException("The properties collection cannot be empty.", nameof(properties));

         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
         Properties = properties;
         _propertyGetterLookup = BuildPropertyGetterLookup(properties);
         _enumerator = entities.GetEnumerator();
      }

      private Dictionary<int, Func<T, object?>> BuildPropertyGetterLookup(IReadOnlyList<IProperty> properties)
      {
         var lookup = new Dictionary<int, Func<T, object?>>();

         for (var i = 0; i < properties.Count; i++)
         {
            var property = properties[i];
            var hasSqlDefaultValue = property.GetDefaultValueSql() != null;
            var hasDefaultValue = property.GetDefaultValue() != null;

            if ((hasSqlDefaultValue || hasDefaultValue) && !property.IsNullable)
            {
               if (property.ClrType.IsClass)
               {
                  _logger.LogWarning("The corresponding column of '{Entity}.{Property}' has a DEFAULT value constraint in the database and is NOT NULL. Dependending on the database vendor the .NET value `null` may lead to an exception because the tool for bulk insert of data may prevent sending `null`s for NOT NULL columns. Use 'MembersToInsert' on 'IBulkInsertOptions' to specify properties to insert and skip '{Entity}.{Property}' so database uses the DEFAULT value.",
                                     property.DeclaringEntityType.ClrType.Name, property.Name, property.DeclaringEntityType.ClrType.Name, property.Name);
               }
               else if (!property.ClrType.IsGenericType ||
                        !property.ClrType.IsGenericTypeDefinition &&
                        property.ClrType.GetGenericTypeDefinition() != typeof(Nullable<>))
               {
                  _logger.LogWarning("The corresponding column of '{Entity}.{Property}' has a DEFAULT value constraint in the database and is NOT NULL. Dependending on the database vendor the \".NET default values\" (`false`, `0`, `00000000-0000-0000-0000-000000000000` etc.) may lead to unexpected results because these values are sent to the database as-is, i.e. the DEFAULT value constraint will NOT be used by database. Use 'MembersToInsert' on 'IBulkInsertOptions' to specify properties to insert and skip '{Entity}.{Property}' so database uses the DEFAULT value.",
                                     property.DeclaringEntityType.ClrType.Name, property.Name, property.DeclaringEntityType.ClrType.Name, property.Name);
               }
            }

            var getter = BuildGetter(property);
            var converter = property.GetValueConverter();
            if (converter != null)
               getter = UseConverter(getter, converter);

            lookup.Add(i, getter);
         }

         return lookup;
      }

      private Func<T, object?> BuildGetter(IProperty property)
      {
         if (property.IsShadowProperty())
         {
            var shadowPropGetter = CreateShadowPropertyGetter(property);
            return shadowPropGetter.GetValue;
         }

         var getter = property.GetGetter();

         if (getter == null)
            throw new ArgumentException($"The property '{property.Name}' of entity '{property.DeclaringEntityType.Name}' has no property getter.");

         return getter.GetClrValue;
      }

      private static Func<T, object?> UseConverter(Func<T, object?> getter, ValueConverter converter)
      {
         var convert = converter.ConvertToProvider;

         return e =>
                {
                   var value = getter(e);

                   if (value != null)
                      value = convert(value);

                   return value;
                };
      }

      private IShadowPropertyGetter CreateShadowPropertyGetter(IProperty property)
      {
         var currentValueGetter = property.GetPropertyAccessors().CurrentValueGetter;
         var shadowPropGetterType = typeof(ShadowPropertyGetter<>).MakeGenericType(property.ClrType);
         var shadowPropGetter = Activator.CreateInstance(shadowPropGetterType, _ctx, currentValueGetter)
                                ?? throw new Exception($"Could not create shadow property getter of type '{shadowPropGetterType.ShortDisplayName()}'.");

         return (IShadowPropertyGetter)shadowPropGetter;
      }

      /// <inheritdoc />
      public int GetPropertyIndex(IProperty entityProperty)
      {
         if (entityProperty == null)
            throw new ArgumentNullException(nameof(entityProperty));

         for (var i = 0; i < Properties.Count; i++)
         {
            if (entityProperty.Equals(Properties[i]))
               return i;
         }

         throw new ArgumentException($"The property '{entityProperty.Name}' of type '{entityProperty.ClrType.ShortDisplayName()}' cannot be read by current reader.");
      }

      /// <inheritdoc />
#pragma warning disable 8766
      public object? GetValue(int i)
#pragma warning restore 8766
      {
         return _propertyGetterLookup[i](_enumerator.Current);
      }

      /// <inheritdoc />
      public bool Read()
      {
         return _enumerator.MoveNext();
      }

      /// <inheritdoc />
      public bool IsDBNull(int i)
      {
         // we are reading entities (.NET objects), there must be no properties of type "DBNull".
         return false;
      }

      /// <inheritdoc />
      public void Dispose()
      {
         _enumerator.Dispose();
      }

      // The following methods are not needed for bulk insert.
      // ReSharper disable ArrangeMethodOrOperatorBody
#pragma warning disable 1591
      object IDataRecord.this[int i] => throw new NotSupportedException();
      object IDataRecord.this[string name] => throw new NotSupportedException();
      int IDataReader.Depth => throw new NotSupportedException();
      int IDataReader.RecordsAffected => throw new NotSupportedException();
      bool IDataReader.IsClosed => throw new NotSupportedException();
      void IDataReader.Close() => throw new NotSupportedException();
      string IDataRecord.GetName(int i) => throw new NotSupportedException();
      string IDataRecord.GetDataTypeName(int i) => throw new NotSupportedException();
      Type IDataRecord.GetFieldType(int i) => throw new NotSupportedException();
      int IDataRecord.GetValues(object[] values) => throw new NotSupportedException();
      int IDataRecord.GetOrdinal(string name) => throw new NotSupportedException();
      bool IDataRecord.GetBoolean(int i) => throw new NotSupportedException();
      byte IDataRecord.GetByte(int i) => throw new NotSupportedException();
      long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
      char IDataRecord.GetChar(int i) => throw new NotSupportedException();
      long IDataRecord.GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException();
      Guid IDataRecord.GetGuid(int i) => throw new NotSupportedException();
      short IDataRecord.GetInt16(int i) => throw new NotSupportedException();
      int IDataRecord.GetInt32(int i) => throw new NotSupportedException();
      long IDataRecord.GetInt64(int i) => throw new NotSupportedException();
      float IDataRecord.GetFloat(int i) => throw new NotSupportedException();
      double IDataRecord.GetDouble(int i) => throw new NotSupportedException();
      string IDataRecord.GetString(int i) => throw new NotSupportedException();
      decimal IDataRecord.GetDecimal(int i) => throw new NotSupportedException();
      DateTime IDataRecord.GetDateTime(int i) => throw new NotSupportedException();
      IDataReader IDataRecord.GetData(int i) => throw new NotSupportedException();
      DataTable IDataReader.GetSchemaTable() => throw new NotSupportedException();
      bool IDataReader.NextResult() => throw new NotSupportedException();
#pragma warning restore 1591
      // ReSharper restore ArrangeMethodOrOperatorBody
   }
}
