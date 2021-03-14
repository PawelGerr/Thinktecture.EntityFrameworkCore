using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.Query.SqlExpressions;
using Thinktecture.EntityFrameworkCore.TempTables;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <summary>
   /// Replaces specific occurrences of <see cref="TableExpression"/> with a <see cref="TempTableExpression"/>.
   /// </summary>
   public class BulkOperationOptimizingVisitor : RelationalOptimizingVisitor
   {
      private readonly IReadOnlyList<TempTableQueryContext> _tempTableQueryContext;

      /// <summary>
      /// Initializes new instance of <see cref="BulkOperationOptimizingVisitor"/>.
      /// </summary>
      /// <param name="parametersValues">Runtime parameters.</param>
      /// <param name="tempTableQueryContext">Temp table contexts to use for finding and replacing of <see cref="TableExpression"/>.</param>
      /// <param name="tableHintContexts"></param>
      public BulkOperationOptimizingVisitor(
         IReadOnlyDictionary<string, object> parametersValues,
         IReadOnlyList<TempTableQueryContext> tempTableQueryContext,
         IReadOnlyList<TableHintContext> tableHintContexts)
         : base(parametersValues, tableHintContexts)
      {
         _tempTableQueryContext = tempTableQueryContext ?? throw new ArgumentNullException(nameof(tempTableQueryContext));
      }

      /// <inheritdoc />
      protected override Expression VisitTable(TableExpression tableExpression)
      {
         var tempTableCtx = _tempTableQueryContext.SingleOrDefault(c => c.Table.Equals(tableExpression));

         if (tempTableCtx is not null)
            return new TempTableExpression(tempTableCtx.TempTableName, tableExpression.Alias);

         return tableExpression;
      }
   }
}
