using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Thinktecture.EntityFrameworkCore.TempTables
{
   /// <summary>
   /// This is an internal API.
   /// </summary>
   public class TempTableQueryContext
   {
      /// <summary>
      /// Table expression to replace with a temp table.
      /// </summary>
      public TableExpression Table { get; }

      /// <summary>
      /// The name of the temp table.
      /// </summary>
      public string TempTableName { get; }

      /// <summary>
      /// Runtime parameter name.
      /// </summary>
      public string ParameterName { get; }

      internal TempTableQueryContext(TableExpression table, string tempTableName, string parameterName)
      {
         Table = table;
         TempTableName = tempTableName;
         ParameterName = parameterName;
      }

      /// <summary>
      /// This is an internal API.
      /// </summary>
      public static bool TryGetTempTableContexts(
         IReadOnlyDictionary<string, object?> parametersValues,
         [NotNullWhen(true)] out IReadOnlyList<TempTableQueryContext>? tempTableQueryContexts)
      {
         List<TempTableQueryContext>? ctxs = null;

         foreach (var (_, value) in parametersValues)
         {
            if (value is not TempTableQueryContext ctx)
               continue;

            ctxs ??= new List<TempTableQueryContext>();
            ctxs.Add(ctx);
         }

         tempTableQueryContexts = ctxs;
         return tempTableQueryContexts is not null;
      }
   }
}
