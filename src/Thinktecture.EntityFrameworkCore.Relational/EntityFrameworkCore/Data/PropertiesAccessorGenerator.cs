using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace Thinktecture.EntityFrameworkCore.Data
{
   /// <summary>
   /// Code generator required by the <see cref="IEntityDataReaderFactory"/>.
   /// </summary>
   public class PropertiesAccessorGenerator : IPropertiesAccessorGenerator
   {
      /// <inheritdoc />
      public Func<T, int, object> CreatePropertiesAccessor<T>(IReadOnlyList<PropertyInfo> properties)
      {
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));
         if (properties.Count == 0)
            throw new ArgumentException("The collection must have at least 1 property.", nameof(properties));

         var entityParam = Expression.Parameter(typeof(T));
         var indexParam = Expression.Parameter(typeof(int));
         var returnTarget = Expression.Label(typeof(object));

         var switchCases = new SwitchCase[properties.Count];

         // generates:
         //   case i:
         //      return entityParam.Property;
         for (var i = 0; i < properties.Count; i++)
         {
            var propertyInfo = properties[i];
            var propertyGetter = Expression.MakeMemberAccess(entityParam, propertyInfo);
            var returnValue = Expression.Return(returnTarget, Expression.Convert(propertyGetter, typeof(object)), typeof(object));

            var switchCase = Expression.SwitchCase(returnValue, Expression.Constant(i, typeof(int)));
            switchCases[i] = switchCase;
         }

         // generates:
         //    throw ArgumentOutOfRangeException(numberOfColumns, index)
         var throwArgOutOfRangeException = GenerateThrowArgOutOfRangeException(properties.Count, indexParam);

         var methodBody = new Expression[]
                          {
                             Expression.Switch(indexParam, throwArgOutOfRangeException, switchCases),

                             // return ...;
                             Expression.Label(returnTarget, Expression.Constant(null, typeof(object)))
                          };

         return Expression.Lambda<Func<T, int, object>>(Expression.Block(methodBody), entityParam, indexParam).Compile();
      }

      [NotNull]
      private static UnaryExpression GenerateThrowArgOutOfRangeException(int numberOfColumns, [NotNull] Expression indexParam)
      {
         var createExceptionMethod = typeof(PropertiesAccessorGenerator).GetMethod(nameof(CreateArgumentOutOfRangeException), BindingFlags.Static | BindingFlags.NonPublic);

         if (createExceptionMethod == null)
            throw new MissingMethodException(nameof(PropertiesAccessorGenerator), nameof(CreateArgumentOutOfRangeException));

         var newException = Expression.Call(null,
                                            createExceptionMethod,
                                            Expression.Constant(numberOfColumns),
                                            indexParam);
         return Expression.Throw(newException, typeof(object));
      }

      [NotNull]
      private static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(int numberOfColumns, int index)
      {
         return new ArgumentOutOfRangeException(nameof(index), $"The temp table has {numberOfColumns} column(s) only. Provided index: {index}");
      }
   }
}
