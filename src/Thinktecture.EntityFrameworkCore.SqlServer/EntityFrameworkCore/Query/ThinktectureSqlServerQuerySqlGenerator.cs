using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;

namespace Thinktecture.EntityFrameworkCore.Query
{
   /// <inheritdoc />
   [SuppressMessage("ReSharper", "EF1001")]
   public class ThinktectureSqlServerQuerySqlGenerator : SqlServerQuerySqlGenerator
   {
      private readonly ITenantDatabaseProvider _databaseProvider;

      /// <inheritdoc />
      public ThinktectureSqlServerQuerySqlGenerator(
         QuerySqlGeneratorDependencies dependencies,
         ITenantDatabaseProvider databaseProvider)
         : base(dependencies)
      {
         _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
      }

      /// <inheritdoc />
      protected override Expression VisitTable(TableExpression tableExpression)
      {
         var databaseName = _databaseProvider.GetDatabaseName(tableExpression.Schema, tableExpression.Name);

         if (!String.IsNullOrWhiteSpace(databaseName))
         {
            Sql.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName))
               .Append(String.IsNullOrWhiteSpace(tableExpression.Schema) ? ".." : ".");
         }

         return base.VisitTable(tableExpression);
      }
   }
}
