using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Thinktecture.EntityFrameworkCore.BulkOperations.Internal;

/// <summary>
/// This is an internal API. It may be changed or removed without notice.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed partial class EfCoreValueExpressionTranslator
{
   private readonly ILogger _logger;
   private readonly DbContext _ctx;

   /// <summary>
   /// Initializes a new instance.
   /// </summary>
   /// <param name="logger">Logger</param>
   /// <param name="ctx">The database context.</param>
   public EfCoreValueExpressionTranslator(
      ILogger logger,
      DbContext ctx)
   {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
   }

   /// <summary>
   /// Translates a bulk update from query into SQL using EF Core's expression translation pipeline.
   /// </summary>
   public (string Sql, IReadOnlyDictionary<string, object?> Parameters) TranslateUpdateFromQuery<TTarget, TSource, TResult>(
      IQueryable<TSource> sourceQuery,
      Expression<Func<TTarget, TResult?>> targetKeySelector,
      Expression<Func<TSource, TResult?>> sourceKeySelector,
      IReadOnlyList<ISetPropertyEntry> setClauseEntries,
      LambdaExpression? filter,
      string? tableNameOverride,
      string? schemaOverride)
      where TTarget : class
      where TSource : class
   {
      var targetEntityType = _ctx.Model.GetEntityType(typeof(TTarget));
      var targetStoreObject = StoreObjectIdentifier.Create(targetEntityType, StoreObjectType.Table)
                              ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{targetEntityType.Name}'.");

      // Step 1: Build the join query
      var joinQuery = _ctx.Set<TTarget>()
                          .Join(sourceQuery,
                                targetKeySelector,
                                sourceKeySelector,
                                (t, s) => new { Target = t, Source = s });

      // Step 2: Extract parameters and compile through EF Core pipeline
      var parameterValues = new Dictionary<string, object?>();
      var (shapedQuery, compilationContext, methodTranslator) = CompileQuery(joinQuery.Expression, parameterValues);

      var selectExpression = (SelectExpression)shapedQuery.QueryExpression;

      // Step 3: Extract entity shapers
      var (targetShaper, sourceShaper) = ExtractEntityShapers<TTarget, TSource>(shapedQuery.ShaperExpression);

      // Step 4: Create SQL translator
      var sqlTranslatorFactory = _ctx.GetService<IRelationalSqlTranslatingExpressionVisitorFactory>();
      var sqlTranslator = sqlTranslatorFactory.Create(compilationContext, methodTranslator);
      var sqlExprFactory = _ctx.GetService<ISqlExpressionFactory>();

      // Step 5: Translate SET entries
      var setters = new List<ColumnValueSetter>();
      var targetTableAlias = GetTargetTableAlias(selectExpression);

      foreach (var entry in setClauseEntries)
      {
         // Resolve target column
         var targetAccess = BulkUpdateFromQueryHelper.ExtractPropertyAccess(entry.TargetPropertySelector);
         var targetProp = BulkUpdateFromQueryHelper.ResolveProperty(targetEntityType, targetAccess);
         var targetColumnName = targetProp.GetColumnName(targetStoreObject)
                                ?? throw new InvalidOperationException($"Could not get column name for property '{targetProp.Name}' on entity type '{targetEntityType.Name}'.");

         var targetColumn = CreateColumnExpression(targetProp, targetColumnName, targetTableAlias);

         // Translate value expression
         var valueExpr = entry.ValueSelector;

         if (valueExpr.Parameters.Count != 2)
            throw new ArgumentException($"Update value expression must have exactly 2 parameters. Expression: {valueExpr}");

         var remapped = RemapTwoParameterLambda(valueExpr, targetShaper, sourceShaper);
         var flattenedRemapped = FlattenClosures(_logger, remapped);

         var sqlValue = sqlTranslator.TranslateProjection(flattenedRemapped)
                        ?? throw new InvalidOperationException($"Could not translate value expression: {valueExpr}");

         if (sqlValue is not SqlExpression sqlExpr)
            throw new InvalidOperationException($"Translated expression is not a SqlExpression: {sqlValue.GetType().Name}");

         // Apply type mapping from target column
         var typeMapping = targetProp.GetRelationalTypeMapping();
         sqlExpr = sqlExprFactory.ApplyTypeMapping(sqlExpr, typeMapping);

         setters.Add(new ColumnValueSetter(targetColumn, sqlExpr));
      }

      // Step 6: Translate filter
      if (filter is not null)
      {
         if (filter.Parameters.Count != 2)
            throw new ArgumentException($"Filter expression must have exactly 2 parameters. Expression: {filter}");

         var remappedFilter = RemapTwoParameterLambda(filter, targetShaper, sourceShaper);
         var flattenedFilter = FlattenClosures(_logger, remappedFilter);

         var sqlFilter = sqlTranslator.TranslateProjection(flattenedFilter)
                         ?? throw new InvalidOperationException($"Could not translate filter expression: {filter}");

         if (sqlFilter is SqlExpression filterExpr)
            selectExpression.ApplyPredicate(filterExpr);
      }

      // Step 7: Clear projections and build UpdateExpression
      // VisitUpdate requires Projection: [] on the SelectExpression.
      selectExpression.ReplaceProjection(new List<Expression>());
      selectExpression.ApplyProjection();

      // The target TableExpression is Tables[0] in the SelectExpression.
      var targetTable = selectExpression.Tables[0];

      if (targetTable is not TableExpression targetTableExpr)
         throw new InvalidOperationException($"Expected TableExpression as first table but got {targetTable.GetType().Name}.");

      var updateExpression = new UpdateExpression(targetTableExpr, selectExpression, setters);

      // Step 8: Render SQL
      var sqlGeneratorFactory = _ctx.GetService<IQuerySqlGeneratorFactory>();
      var command = sqlGeneratorFactory.Create().GetCommand(updateExpression);

      var sql = command.CommandText;

      // Step 9: Apply table/schema override via string replacement
      if (tableNameOverride is not null || schemaOverride is not null)
         sql = ApplyTableSchemaAndNameOverride(sql, tableNameOverride, schemaOverride, targetEntityType);

      return (sql, parameterValues);
   }

   private string ApplyTableSchemaAndNameOverride(
      string sql,
      string? tableNameOverride,
      string? schemaOverride,
      IEntityType targetEntityType)
   {
      var sqlGenerationHelper = _ctx.GetService<ISqlGenerationHelper>();
      var originalName = targetEntityType.GetTableName()!;
      var originalSchema = targetEntityType.GetSchema();

      var originalQualified = sqlGenerationHelper.DelimitIdentifier(originalName, originalSchema);
      var overriddenQualified = sqlGenerationHelper.DelimitIdentifier(
                                                                      tableNameOverride ?? originalName,
                                                                      schemaOverride ?? originalSchema);

      var updatedSql = sql.Replace(originalQualified, overriddenQualified);

      if (ReferenceEquals(sql, updatedSql))
         throw new InvalidOperationException($"Table/schema override failed: could not find '{originalQualified}' in the generated UPDATE SQL.");

      return updatedSql;
   }

   /// <summary>
   /// Translates a bulk insert from query into SQL using EF Core's expression translation pipeline.
   /// </summary>
   public (string Sql, IReadOnlyDictionary<string, object?> Parameters) TranslateInsertFromQuery<TTarget, TSource>(
      IQueryable<TSource> sourceQuery,
      IReadOnlyList<ISetPropertyEntry> entries,
      string? tableNameOverride,
      string? schemaOverride)
      where TTarget : class
      where TSource : class
   {
      var targetEntityType = _ctx.Model.GetEntityType(typeof(TTarget));
      var targetStoreObject = StoreObjectIdentifier.Create(targetEntityType, StoreObjectType.Table)
                              ?? throw new InvalidOperationException($"Could not create StoreObjectIdentifier for table '{targetEntityType.Name}'.");

      // Step 1: Compile source query through EF Core pipeline
      var parameterValues = new Dictionary<string, object?>();
      var (shapedQuery, compilationContext, methodTranslator) = CompileQuery(sourceQuery.Expression, parameterValues);

      var selectExpression = (SelectExpression)shapedQuery.QueryExpression;

      // Step 2: Extract source entity shaper
      var sourceShaper = ExtractSingleEntityShaper<TSource>(shapedQuery.ShaperExpression);

      // Step 3: Create SQL translator
      var sqlTranslatorFactory = _ctx.GetService<IRelationalSqlTranslatingExpressionVisitorFactory>();
      var sqlTranslator = sqlTranslatorFactory.Create(compilationContext, methodTranslator);
      var sqlExprFactory = _ctx.GetService<ISqlExpressionFactory>();

      // Step 4: Build target column list and translate value expressions
      var sqlGenerationHelper = _ctx.GetService<ISqlGenerationHelper>();
      var targetColumns = new List<string>();
      var valueExpressions = new List<SqlExpression>();

      foreach (var entry in entries)
      {
         // Resolve target column name
         var targetAccess = BulkUpdateFromQueryHelper.ExtractPropertyAccess(entry.TargetPropertySelector);
         var targetProp = BulkUpdateFromQueryHelper.ResolveProperty(targetEntityType, targetAccess);
         var targetColumnName = targetProp.GetColumnName(targetStoreObject)
                                ?? throw new InvalidOperationException($"Could not get column name for property '{targetProp.Name}' on entity type '{targetEntityType.Name}'.");

         targetColumns.Add(sqlGenerationHelper.DelimitIdentifier(targetColumnName));

         // Translate value expression (1 parameter: source)
         var valueExpr = entry.ValueSelector;

         if (valueExpr.Parameters.Count != 1)
            throw new ArgumentException($"Insert value expression must have exactly 1 parameter. Expression: {valueExpr}");

         var remapped = RemapSingleParameterLambda(valueExpr, sourceShaper);
         var flattenedRemapped = FlattenClosures(_logger, remapped);

         var sqlValue = sqlTranslator.TranslateProjection(flattenedRemapped)
                        ?? throw new InvalidOperationException($"Could not translate value expression: {valueExpr}");

         if (sqlValue is not SqlExpression sqlExpr)
            throw new InvalidOperationException($"Translated expression is not a SqlExpression: {sqlValue.GetType().Name}");

         // Apply type mapping from target column
         var typeMapping = targetProp.GetRelationalTypeMapping();
         sqlExpr = sqlExprFactory.ApplyTypeMapping(sqlExpr, typeMapping);

         valueExpressions.Add(sqlExpr);
      }

      // Step 5: Convert SqlConstantExpressions to SqlParameterExpressions with unique names
      // to prevent SelectExpression.ApplyProjection() from deduplicating identical constants
      // (e.g., two columns both mapped to 0 would otherwise collapse into a single projection).
      var projectionExpressions = new List<SqlExpression>(valueExpressions.Count);
      var constParamIndex = 0;

      foreach (var valExpr in valueExpressions)
      {
         if (valExpr is SqlConstantExpression constExpr)
         {
            var paramName = $"__insert_const_{constParamIndex++}";
            parameterValues[paramName] = constExpr.Value;
            projectionExpressions.Add(new SqlParameterExpression(paramName, valExpr.Type, constExpr.TypeMapping));
         }
         else
         {
            projectionExpressions.Add(valExpr);
         }
      }

      // Step 6: Replace projections in SelectExpression with our value expressions
      selectExpression.ReplaceProjection(projectionExpressions.Cast<Expression>().ToList());
      selectExpression.ApplyProjection();

      // Step 7: Render SELECT SQL
      var sqlGeneratorFactory = _ctx.GetService<IQuerySqlGeneratorFactory>();
      var selectCommand = sqlGeneratorFactory.Create().GetCommand(selectExpression);

      // Step 8: Build INSERT INTO ... SELECT ...
      var tableName = tableNameOverride ?? targetEntityType.GetTableName()
                      ?? throw new InvalidOperationException($"The entity '{targetEntityType.Name}' has no table name.");
      var schema = schemaOverride ?? targetEntityType.GetSchema();

      var insertSql = $"INSERT INTO {sqlGenerationHelper.DelimitIdentifier(tableName, schema)} ({string.Join(", ", targetColumns)})\n{selectCommand.CommandText}";

      return (insertSql, parameterValues);
   }

   private (ShapedQueryExpression ShapedQuery, QueryCompilationContext CompilationContext, QueryableMethodTranslatingExpressionVisitor MethodTranslator)
      CompileQuery(Expression queryExpression, Dictionary<string, object?> parameterValues)
   {
      // Extract parameters using EF Core's ExpressionTreeFuncletizer (via QueryCompiler)
      var queryCompiler = _ctx.GetService<IQueryCompiler>();
      var logger = _ctx.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();

      if (queryCompiler is not QueryCompiler concreteCompiler)
         throw new InvalidOperationException($"Expected QueryCompiler but got {queryCompiler.GetType().Name}.");

      var extractedQuery = concreteCompiler.ExtractParameters(queryExpression, parameterValues, logger);

      // Compile through pipeline: preprocess → translate
      var compilationContextFactory = _ctx.GetService<IQueryCompilationContextFactory>();
      var compilationContext = compilationContextFactory.Create(false);

      var preprocessorFactory = _ctx.GetService<IQueryTranslationPreprocessorFactory>();
      var preprocessed = preprocessorFactory.Create(compilationContext).Process(extractedQuery);

      var methodTranslatorFactory = _ctx.GetService<IQueryableMethodTranslatingExpressionVisitorFactory>();
      var methodTranslator = methodTranslatorFactory.Create(compilationContext);
      var translated = methodTranslator.Translate(preprocessed);

      if (translated is not ShapedQueryExpression shapedQuery)
         throw new InvalidOperationException($"Expected ShapedQueryExpression but got {translated?.GetType().Name ?? "null"}");

      return (shapedQuery, compilationContext, methodTranslator);
   }

   private static (Expression TargetShaper, Expression SourceShaper) ExtractEntityShapers<TTarget, TSource>(Expression shaperExpression)
   {
      var shapers = CollectStructuralTypeShapers(shaperExpression);

      Expression? targetShaper = null;
      Expression? sourceShaper = null;

      foreach (var (shaper, clrType) in shapers)
      {
         if (typeof(TTarget).IsAssignableFrom(clrType) && targetShaper is null)
            targetShaper = shaper;
         else if (typeof(TSource).IsAssignableFrom(clrType) && sourceShaper is null)
            sourceShaper = shaper;
      }

      // If TTarget == TSource (self-join), assign based on order
      if (targetShaper is not null && sourceShaper is null && typeof(TTarget) == typeof(TSource) && shapers.Count >= 2)
         sourceShaper = shapers[1].Shaper;

      return (targetShaper ?? throw new InvalidOperationException($"Could not find entity shaper for target type '{typeof(TTarget).Name}'."),
              sourceShaper ?? throw new InvalidOperationException($"Could not find entity shaper for source type '{typeof(TSource).Name}'."));
   }

   private static Expression ExtractSingleEntityShaper<TSource>(Expression shaperExpression)
   {
      var shapers = CollectStructuralTypeShapers(shaperExpression);

      foreach (var (shaper, clrType) in shapers)
      {
         if (typeof(TSource).IsAssignableFrom(clrType))
            return shaper;
      }

      // For non-entity sources (anonymous types, projections), use the shaper expression directly
      if (typeof(TSource).IsAssignableFrom(shaperExpression.Type))
         return shaperExpression;

      throw new InvalidOperationException($"Could not find entity shaper for type '{typeof(TSource).Name}'.");
   }

   private static List<(Expression Shaper, Type ClrType)> CollectStructuralTypeShapers(Expression expression)
   {
      var results = new List<(Expression Shaper, Type ClrType)>();
      CollectStructuralTypeShapers(expression, results);

      return results;
   }

   private static void CollectStructuralTypeShapers(Expression expression, List<(Expression Shaper, Type ClrType)> results)
   {
      while (true)
      {
         switch (expression)
         {
            case StructuralTypeShaperExpression shaper:
               results.Add((shaper, shaper.Type));
               break;

            case NewExpression newExpr:
               foreach (var arg in newExpr.Arguments)
               {
                  CollectStructuralTypeShapers(arg, results);
               }

               // Also add the NewExpression itself so non-entity types
               // (e.g., anonymous type projections) can be matched by CLR type.
               results.Add((newExpr, newExpr.Type));
               break;

            case MemberInitExpression memberInit:
               CollectStructuralTypeShapers(memberInit.NewExpression, results);

               foreach (var binding in memberInit.Bindings)
               {
                  if (binding is not MemberAssignment assignment)
                     continue;

                  var countBefore = results.Count;
                  CollectStructuralTypeShapers(assignment.Expression, results);

                  // If no structural type shapers were found in this binding,
                  // collect the binding expression itself (e.g., anonymous type projection)
                  if (results.Count == countBefore)
                     results.Add((assignment.Expression, assignment.Expression.Type));
               }

               break;

            case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
               expression = unary.Operand;
               continue;

            case MemberExpression { Expression: not null } member:
               expression = member.Expression;
               continue;
         }

         break;
      }
   }

   private static Expression RemapTwoParameterLambda(LambdaExpression lambda, Expression targetShaper, Expression sourceShaper)
   {
      return ReplacingExpressionVisitor.Replace([lambda.Parameters[0], lambda.Parameters[1]],
                                                [targetShaper, sourceShaper],
                                                lambda.Body);
   }

   private static Expression RemapSingleParameterLambda(LambdaExpression lambda, Expression sourceShaper)
   {
      return ReplacingExpressionVisitor.Replace(lambda.Parameters[0], sourceShaper, lambda.Body);
   }

   private static Expression FlattenClosures(ILogger logger, Expression expression)
   {
      return new ClosureFlatteningVisitor(logger).Visit(expression);
   }

   private static string GetTargetTableAlias(SelectExpression selectExpression)
   {
      return selectExpression.Tables[0].GetRequiredAlias();
   }

   private static ColumnExpression CreateColumnExpression(
      IProperty property,
      string columnName,
      string tableAlias)
   {
      var typeMapping = property.GetRelationalTypeMapping();

      return new ColumnExpression(columnName,
                                  tableAlias,
                                  property.ClrType,
                                  typeMapping,
                                  property.IsNullable);
   }

   private sealed partial class ClosureFlatteningVisitor(ILogger logger) : ExpressionVisitor
   {
      protected override Expression VisitMember(MemberExpression node)
      {
         var expression = Visit(node.Expression);

         if (expression is not ConstantExpression constant)
            return node.Update(expression);

         try
         {
            var value = node.Member switch
            {
               FieldInfo fi => fi.GetValue(constant.Value),
               PropertyInfo pi => pi.GetValue(constant.Value),
               _ => throw new NotSupportedException($"Unsupported member type: {node.Member.GetType().Name}")
            };

            return Expression.Constant(value, node.Type);
         }
         catch (TargetInvocationException ex)
         {
            LogClosureFlatteningFailed(logger, ex, node.Member.Name, node.Member.DeclaringType?.Name);
         }

         return node.Update(expression);
      }

      [LoggerMessage(Level = LogLevel.Warning,
                     Message = "ClosureFlatteningVisitor: Failed to evaluate member '{MemberName}' on type '{MemberDeclaringTypeName}'.")]
      private static partial void LogClosureFlatteningFailed(ILogger logger, Exception ex, string memberName, string? memberDeclaringTypeName);
   }
}
