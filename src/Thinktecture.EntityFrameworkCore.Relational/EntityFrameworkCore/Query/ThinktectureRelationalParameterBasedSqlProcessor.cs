using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Extends <see cref="RelationalParameterBasedSqlProcessor"/>.
   /// </summary>
   public class ThinktectureRelationalParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
   {
      /// <inheritdoc />
      public ThinktectureRelationalParameterBasedSqlProcessor(
         RelationalParameterBasedSqlProcessorDependencies dependencies,
         bool useRelationalNulls)
         : base(dependencies, useRelationalNulls)
      {
      }

      /// <inheritdoc />
      protected override SelectExpression ProcessSqlNullability(SelectExpression selectExpression, IReadOnlyDictionary<string, object> parametersValues, out bool canCache)
      {
         if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));
         if (parametersValues == null)
            throw new ArgumentNullException(nameof(parametersValues));

         return new ThinktectureSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(selectExpression, parametersValues, out canCache);
      }
   }
}
