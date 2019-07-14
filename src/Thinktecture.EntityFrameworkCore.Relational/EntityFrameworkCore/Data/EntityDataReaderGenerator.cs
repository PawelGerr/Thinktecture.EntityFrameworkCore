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
   public class EntityDataReaderGenerator : IEntityDataReaderGenerator
   {
      /// <inheritdoc />
      public Func<T, int, object> CreatePropertiesAccessor<T>(IReadOnlyList<PropertyInfo> properties)
      {
         if (properties == null)
            throw new ArgumentNullException(nameof(properties));

         var entityParam = Expression.Parameter(typeof(T));
         var indexParam = Expression.Parameter(typeof(int));
         var returnTarget = Expression.Label(typeof(object));

         var bodyExpressions = new List<Expression>();

         // generates:
         // if(indexParam == i)
         //   return entityParam.Property;
         for (var i = 0; i < properties.Count; i++)
         {
            var propertyInfo = properties[i];
            var propertyGetter = Expression.MakeMemberAccess(entityParam, propertyInfo);
            var returnValue = Expression.Return(returnTarget, Expression.Convert(propertyGetter, typeof(object)), typeof(object));

            var ifExpression = Expression.IfThen(Expression.Equal(indexParam, Expression.Constant(i)), returnValue);
            bodyExpressions.Add(ifExpression);
         }

         // generates:
         //    throw IndexOutOfRangeException(numberOfColumns, index)
         var throwIndexOutOfRangeException = GenerateThrowIndexOutOfRangeException(properties.Count, indexParam);
         bodyExpressions.Add(throwIndexOutOfRangeException);

         // return ...;
         bodyExpressions.Add(Expression.Label(returnTarget, Expression.Constant(null, typeof(object))));

         return Expression.Lambda<Func<T, int, object>>(Expression.Block(bodyExpressions), entityParam, indexParam).Compile();
      }

      [NotNull]
      private static UnaryExpression GenerateThrowIndexOutOfRangeException(int numberOfColumns, [NotNull] ParameterExpression indexParam)
      {
         var createExceptionMethod = typeof(EntityDataReaderGenerator).GetMethod(nameof(CreateArgumentOutOfRangeException), BindingFlags.Static | BindingFlags.NonPublic);

         if (createExceptionMethod == null)
            throw new MissingMethodException(nameof(EntityDataReaderGenerator), nameof(CreateArgumentOutOfRangeException));

         var newException = Expression.Call(null,
                                            createExceptionMethod,
                                            Expression.Constant(numberOfColumns),
                                            indexParam);
         return Expression.Throw(newException);
      }

      [NotNull]
      private static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(int numberOfColumns, int index)
      {
         return new ArgumentOutOfRangeException($"The temp table has {numberOfColumns} column(s) only. Provided index: {index}");
      }
   }
}
