using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// Data reader for <see cref="ITempTable{TColumn1}"/>.
   /// </summary>
   /// <typeparam name="T">Type of the entity.</typeparam>
   /// <typeparam name="TColumn1">Type of the column.</typeparam>
   public class TempTableDataReader<T, TColumn1> : TempTableDataReaderBase<T>
      where T : class, ITempTable<TColumn1>
   {
      // ReSharper disable once StaticMemberInGenericType
      private static readonly PropertyInfo _column1PropertyInfo;

      static TempTableDataReader()
      {
         var type = typeof(T);
         _column1PropertyInfo = type.GetProperty(nameof(ITempTable<TColumn1>.Column1), typeof(TColumn1));
      }

      /// <inheritdoc />
      public TempTableDataReader([NotNull] IEnumerable<T> entities)
         : base(entities)
      {
      }

      /// <inheritdoc />
      public override int FieldCount => 1;

      /// <inheritdoc />
      public override int GetPropertyIndex(PropertyInfo propertyInfo)
      {
         if (propertyInfo == _column1PropertyInfo)
            return 0;

         throw new ArgumentException($"The property '{propertyInfo.Name}' of type '{propertyInfo.PropertyType.DisplayName()}' is not a member of type '{typeof(T).DisplayName()}'.");
      }

      /// <inheritdoc />
      public override object GetValue(int i)
      {
         if (i != 0)
            throw new IndexOutOfRangeException($"The temp table has 1 column only. Provided index: {i}");

         return Current.Column1;
      }
   }

   /// <summary>
   /// Data reader for <see cref="ITempTable{TColumn1,TColumn2}"/>.
   /// </summary>
   /// <typeparam name="T">Type of the entity.</typeparam>
   /// <typeparam name="TColumn1">Type of the column 1.</typeparam>
   /// <typeparam name="TColumn2">Type of the column 2.</typeparam>
   public class TempTableDataReader<T, TColumn1, TColumn2> : TempTableDataReaderBase<T>
      where T : class, ITempTable<TColumn1, TColumn2>
   {
      // ReSharper disable StaticMemberInGenericType

      private static readonly PropertyInfo _column1PropertyInfo;
      private static readonly PropertyInfo _column2PropertyInfo;

      // ReSharper restore StaticMemberInGenericType

      static TempTableDataReader()
      {
         var type = typeof(T);
         _column1PropertyInfo = type.GetProperty(nameof(ITempTable<TColumn1, TColumn2>.Column1), typeof(TColumn1));
         _column2PropertyInfo = type.GetProperty(nameof(ITempTable<TColumn1, TColumn2>.Column2), typeof(TColumn2));
      }

      /// <inheritdoc />
      public TempTableDataReader([NotNull] IEnumerable<T> entities)
         : base(entities)
      {
      }

      /// <inheritdoc />
      public override int FieldCount => 2;

      /// <inheritdoc />
      public override int GetPropertyIndex(PropertyInfo propertyInfo)
      {
         if (propertyInfo == _column1PropertyInfo)
            return 0;

         if (propertyInfo == _column2PropertyInfo)
            return 1;

         throw new ArgumentException($"The property '{propertyInfo.Name}' of type '{propertyInfo.PropertyType.DisplayName()}' is not a member of type '{typeof(T).DisplayName()}'.");
      }

      /// <inheritdoc />
      public override object GetValue(int i)
      {
         if (i == 0)
            return Current.Column1;

         if (i == 1)
            return Current.Column2;

         throw new IndexOutOfRangeException($"The temp table has 2 columns only. Provided index: {i}");
      }
   }
}
